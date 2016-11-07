using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;

namespace MinVsNunit
{
    public class ProcessManager
    {
        private readonly OutputWindowPane outputWindowPane;
        private readonly Debugger2 debugger;
        private readonly object processLock;
        private readonly Regex regexPath;
        private System.Diagnostics.Process process;

        public ProcessManager(OutputWindowPane outputWindowPane, Debugger2 debugger)
        {
            this.outputWindowPane = outputWindowPane;
            this.debugger = debugger;
            this.processLock = new object();
            this.regexPath = new Regex(@" in ([A-Za-z]:[^:]*):line (\d+)$", RegexOptions.Compiled);
        }

        public bool StartNunit(string nunitPath, IEnumerable<string> assemblyPaths, string testToRun, bool attachDebugger)
        {
            if (this.process != null)
            {
                return false;
            }

            lock (this.processLock)
            {
                if (this.process != null)
                {
                    return false;
                }

                this.outputWindowPane.Clear();
                this.outputWindowPane.Activate();

                var args = new StringBuilder();
                args.Append("/labels /process:Separate /domain:Multiple ");
                if (testToRun != null)
                {
                    args.AppendFormat("/run:{0} ", testToRun);
                }

                foreach (var assemblyPath in assemblyPaths)
                {
                    args.Append('"');
                    args.Append(assemblyPath);
                    args.Append('"');
                    args.Append(' ');
                }

                var startInfo = new ProcessStartInfo();
                startInfo.FileName = nunitPath; // @"C:\Program Files (x86)\NUnit 2.6.4\bin\nunit-console.exe";
                startInfo.Arguments = args.ToString();
                startInfo.UseShellExecute = false;
                //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;

                this.process = new System.Diagnostics.Process();
                process.StartInfo = startInfo;
                //this.process = System.Diagnostics.Process.Start(startInfo);
                process.OutputDataReceived += this.ProcessOutputDataReceived;
                process.Exited += this.ProcessExited;
                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();

                if (attachDebugger)
                {
                    AttachDebugger();
                }

                return true;
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            string lineToWrite;
            {
                var match = this.regexPath.Match(e.Data);
                if (match.Success)
                {
                    int indentEndIndex = 0;
                    while (indentEndIndex < match.Index && e.Data[indentEndIndex] == ' ')
                    {
                        indentEndIndex++;
                    }

                    lineToWrite = string.Format(
                        "{0}{1}({2}): {3}",
                        e.Data.Substring(0, indentEndIndex),
                        match.Groups[1],
                        match.Groups[2],
                        e.Data.Substring(indentEndIndex, match.Index - indentEndIndex));
                }
                else
                {
                    lineToWrite = e.Data;
                }
            }

            this.WriteLine(lineToWrite);
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            this.WriteLine("---done---");
            lock (this.processLock)
            {
                this.process = null;
            }
        }

        private void WriteLine(string line)
        {
            this.outputWindowPane.OutputString(line);
            this.outputWindowPane.OutputString(Environment.NewLine);
        }

        private void AttachDebugger()
        {
            int tryCount = 50;
            while (tryCount-- > 0)
            {
                try
                {
                    Process2 procToAttachTo = null;
                    foreach (Process2 proc in this.debugger.LocalProcesses)
                    {
                        //if (proc.ProcessID == this.process.Id)
                        if (proc.Name.EndsWith("nunit-agent.exe") || proc.Name.EndsWith("nunit-agent-x86.exe"))
                        {
                            procToAttachTo = proc;
                            break;
                        }
                    }

                    if (procToAttachTo != null)
                    {
                        procToAttachTo.Attach();
                        this.WriteLine("Debugger attached. Current mode: " + this.debugger.CurrentMode);
                        break;
                    }
                    else
                    {
                        this.WriteLine("No process found to attach to. " + tryCount + " tries left...");
                    }
                }
                catch (COMException)
                {
                    this.WriteLine("Failed to attach debugger. " + tryCount + " tries left...");
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
