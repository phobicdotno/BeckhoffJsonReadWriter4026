Use the project in this folder in order to get the example running.

I assume you already prepared your PC or PLC in the right way by doing the following things:

- Install TFU0003 in your target
- Reference the BeckhoffJsonReadWriter4026 library in your twincat project

If not:

Download and reference the [BeckhoffJsonReadWriter4026 library](https://github.com/phobicdotno/BeckhoffJsonReadWriter4026/releases/latest) and import it to your project.

![](https://github.com/phobicdotno/BeckhoffJsonReadWriter4026/raw/master/docs/library.png)

After thar you can start using the BeckhoffJsonReadWriter4026.

# Usage

The usage if the example is very simple a screen recording will show you how to write and read json files

![](https://github.com/phobicdotno/BeckhoffJsonReadWriter4026/raw/master/docs/example_in_action.gif)

# Troubleshooting

If you are still wondering what is going wrong you may probably want to see the log of the function.
For enabling the log just copy the [log.config](https://github.com/phobicdotno/BeckhoffJsonReadWriter4026/raw/master/TFU003/TFU003/log.config") into the function folder: `C:\ProgramData\Beckhoff\Functions\Unofficial\JsonReadWriter4026`.
Re-execute the parser for generate logs into `C:\ProgramData\Beckhoff\Functions\Unofficial\JsonReadWriter4026\logs`.
