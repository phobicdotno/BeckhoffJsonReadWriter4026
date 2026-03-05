using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using TFU001;
using TwinCAT.Ads;
using TwinCAT.JsonExtension;

namespace TFU003
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Argument(0, "File")]
        public string FilePath { get; set; }
        [Argument(1, "Method")] public FileOperationMethod Method { get; set; }
        [Argument(2, "Field")] public string Field { get; set; }
        [Option(ShortName = "AdsNetId")] public string AdsNetId { get; set; } = "";
        [Option(ShortName = "AdsPort")] public int AdsPort { get; set; } = 851;

        private static void CreateLogger()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log.config"));
        }

        /// <summary>
        /// Extracts a JArray from a wrapper JObject for ARRAY targets.
        /// ReadJson produces {"VarName": [...]} but WriteJson(JObject) passes
        /// empty jsonName to WriteRecursive, so SelectToken("") as JArray fails.
        /// This method detects that case and returns the inner JArray directly.
        /// </summary>
        private static JArray TryExtractArray(JObject obj, string variablePath, ILog logger)
        {
            // Get the last segment of the PLC path (e.g. "OperatorInfo" from "PLC1.AutoTestLoop.OperatorInfo")
            var varName = variablePath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";

            // Check if the JObject has a single property matching the variable name that contains a JArray
            if (obj.Count == 1)
            {
                var prop = obj.Properties().First();
                if (prop.Value is JArray array)
                {
                    logger.Debug($"Detected array wrapper: property '{prop.Name}' contains JArray with {array.Count} elements");
                    return array;
                }
            }

            // Also check by variable name even if there are multiple properties
            var token = obj[varName];
            if (token is JArray namedArray)
            {
                logger.Debug($"Detected array wrapper: property '{varName}' contains JArray with {namedArray.Count} elements");
                return namedArray;
            }

            return null;
        }

        public async Task OnExecute()
        {
            var adsClient = new AdsClient();
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            CreateLogger();
            var logger = LoggerFactory.GetLogger();

            try
            {

                logger.Debug("Starting Json Read Writer");
                logger.Debug($"Connecting to Beckhoff Port: {AdsPort} - AdsNet: '{AdsNetId}'");
                if(string.IsNullOrEmpty(AdsNetId))
                    adsClient.Connect(AdsPort);
                else
                   adsClient.Connect(AdsNetId, AdsPort);

                logger.Debug($"Method: {Method}");

                logger.Debug($"File: {FilePath}");

                logger.Debug($"Field: {Field}");

                logger.Debug($"Executing...");

                if (Method == FileOperationMethod.Write || Method == FileOperationMethod.ReadFile)
                {
                    var content = File.ReadAllText(FilePath);
                    logger.Debug($"Writing json into {Field}...");
                    var objectResponse = JObject.Parse(content);

                    // Fix: detect ARRAY OF STRUCT targets where ReadJson wrapped the
                    // array in a JObject like {"VarName": [...]}. The library's
                    // WriteRecursive uses SelectToken("") as JArray which returns null
                    // for this structure. Extract the inner JArray and call the array
                    // overload of WriteJson instead.
                    var innerArray = TryExtractArray(objectResponse, Field, logger);
                    if (innerArray != null)
                    {
                        logger.Debug($"Using WriteJson(JArray) overload for array target");
                        await adsClient.WriteJson(Field, innerArray);
                    }
                    else
                    {
                        await adsClient.WriteJson(Field, objectResponse);
                    }
                }
                else if (Method == FileOperationMethod.Read || Method == FileOperationMethod.WriteFile)
                {
                    var json = await adsClient.ReadJson(Field);
                    File.WriteAllText(FilePath, json.ToString());
                }
                else
                {
                    logger.Warn("Invalid Method");
                }

                adsClient.Disconnect();
            }
            catch (Exception e)
            {
                logger.Error($"Error while calling Json Parser: {e}", e);
                logger.Error($"{e.StackTrace}");
            }
            finally
            {
                adsClient?.Dispose();
            }
        }
    }

    public enum FileOperationMethod
    {
        [Obsolete("Use ReadFile")]Read,
        [Obsolete("Use WriteFile")]Write,
        ReadFile,
        WriteFile
    }
}
