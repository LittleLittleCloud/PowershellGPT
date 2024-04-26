using AutoGen.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
namespace PowershellGPT;

internal class PowershellRunnerAgent : IAgent
{
    public PowershellRunnerAgent(string name)
    {
        Name = name;
    }
    public string Name { get; }

    public Task<IMessage> GenerateReplyAsync(IEnumerable<IMessage> messages, GenerateReplyOptions? options = null, CancellationToken cancellationToken = default)
    {
        // get the last message
        var lastMessage = messages.Last() ?? throw new ArgumentException("No message to reply to");

        // retrieve the powershell script from the last message
        // the script will be placed between ```pwsh and ``` so we need to extract it
        var content = lastMessage.GetContent();
        if (content is string contentString)
        {
            var script = contentString
                .Split("```pwsh")
                .Last()
                .Split("```")
                .First()
                .Trim();

            var powershell = PowerShell.Create().AddScript(script);
            powershell.Invoke();
            if (powershell.HadErrors)
            {
                var errorMessage = powershell.Streams.Error.Select(e => e.ToString()).Aggregate((a, b) => $"{a}\n{b}");
                errorMessage = @$"[ERROR]
{errorMessage}";
                return Task.FromResult<IMessage>(new TextMessage(Role.Assistant, errorMessage, from: this.Name));
            }
            else
            {
                var successMessage = powershell.Streams.Information?.Select(e => e.ToString()).Aggregate((a, b) => $"{a}\n{b}") ?? "no output";
                successMessage = @$"[SUCCESS]
{successMessage}";

                return Task.FromResult<IMessage>(new TextMessage(Role.Assistant, successMessage, from: this.Name));
            }
//            // save the script to a temporary file which has a .ps1 extension
//            var tempFile = Path.GetTempFileName();
//            tempFile = Path.ChangeExtension(tempFile, ".ps1");
//            File.WriteAllText(tempFile, script);
//            // run the powershell script by starting a new process
//            var process = new Process
//            {
//                StartInfo = new ProcessStartInfo
//                {
//                    FileName = "powershell.exe",
//                    Arguments = $"-File {tempFile}",
//                    //Arguments = $"-Command {script}",
//                    RedirectStandardOutput = true,
//                    RedirectStandardError = true,
//                    UseShellExecute = false,
//                    CreateNoWindow = true,
//                }
//            };

//            // write output and error to the console and string builder
//            var output = new StringBuilder();
//            var error = new StringBuilder();
//            process.OutputDataReceived += (sender, e) =>
//            {
//                if (!string.IsNullOrEmpty(e.Data))
//                {
//                    Console.WriteLine(e.Data);
//                    output.AppendLine(e.Data);
//                }
//            };

//            process.ErrorDataReceived += (sender, e) =>
//            {
//                if (!string.IsNullOrEmpty(e.Data))
//                {
//                    Console.WriteLine(e.Data);
//                    error.AppendLine(e.Data);
//                }
//            };

//            process.Start();
//            process.BeginOutputReadLine();
//            process.BeginErrorReadLine();
//            process.WaitForExit();

//            // if there is an error, return the error message
            
//            if (error.Length > 0)
//            {
//                var errorMessage = error.ToString();
//                errorMessage = @$"[ERROR]
//{errorMessage}";
//                return Task.FromResult<IMessage>(new TextMessage(Role.Assistant, errorMessage));
//            }
//            else
//            {
//                var successMessage = output.ToString();
//                successMessage = @$"[SUCCESS]
//{successMessage}";
//                return Task.FromResult<IMessage>(new TextMessage(Role.Assistant, successMessage));
//            }
        }
        else
        {
            throw new ArgumentException("Invalid message content");
        }
    }
}
