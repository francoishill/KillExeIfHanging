using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace KillExeIfHanging
{
	class Program
	{
		private static void ShowError(string err)
		{
			MessageBox.Show(err, "Error in KillExeIfHanging", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private static readonly string cUsageExample = 
			"Usage as follows:"
			+ Environment.NewLine
			+ "KillExeIfHanging.exe -minuteshanging -procname1 -procname2 ... -procnameN"
			+ Environment.NewLine
			+ Environment.NewLine
			+ "Example would be:"
			+ Environment.NewLine
			+ "KillExeIfHanging.exe -60 -git -ssh";

		private static int tmpMinutesHanging;
		private static List<string> procnames;
		private static readonly char[] cAllowedArgumentsStartChars = new char[] { '-', '/' };
		static int Main(string[] args)
		{
			if (args.Length == 0 || args.Length < 2)//We need the MinutesHanging and at least ONE processname
			{
				ShowError("Please specify at least 2 command-line arguments (" + args.Length + " given)."
					+ Environment.NewLine
					+ Environment.NewLine
					+ cUsageExample);
				return 1;
			}

			string argumentforMinutesHanging = args[0].TrimStart(cAllowedArgumentsStartChars);
			if (!int.TryParse(argumentforMinutesHanging, out tmpMinutesHanging))
			{
				ShowError("Unable to parse first argument to int for MinutesHanging, argument = '" + argumentforMinutesHanging + "'. "
					+ cUsageExample);
				return 1;
			}

			procnames = new List<string>();
			for (int i = 1; i < args.Length; i++)
				procnames.Add(args[i].TrimStart(cAllowedArgumentsStartChars));

			Timer timerCheckForProcessesHanging = new Timer();
			timerCheckForProcessesHanging.Interval = 5000;//1000 * 60;//Minutely
			timerCheckForProcessesHanging.Tick += new EventHandler(checkForProcessesHanging_Tick);
			timerCheckForProcessesHanging.Start();

			while (timerCheckForProcessesHanging.Enabled)
				Application.DoEvents();

			return 0;
		}

		private static void checkForProcessesHanging_Tick(object sender, EventArgs e)
		{
			foreach (var pn in procnames)
			{
				try
				{
					string procNameWithExe = pn + (pn.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) ? "" : ".exe");
					string procNameNoExe = pn.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) ? pn.Substring(0, pn.Length - ".exe".Length) : pn;
					var procs = Process.GetProcessesByName(procNameWithExe);
					if (procs.Length == 0)
						procs = Process.GetProcessesByName(procNameNoExe);

					foreach (var proc in procs)
					{
						try
						{
							if (DateTime.Now.Subtract(proc.StartTime).TotalMinutes >= tmpMinutesHanging)
								proc.Kill();
						}
						catch (Exception exc)
						{
							ShowError("Exception in killing process '" + pn + "': " + exc.Message
								+ Environment.NewLine + Environment.NewLine
								+ "Stacktrace:"
								+ Environment.NewLine
								+ exc.StackTrace);
						}
					}
				}
				catch (Exception exc)
				{
					ShowError("Exception in processing commands to kill Processes for '" + pn + "': " + exc.Message
						+ Environment.NewLine + Environment.NewLine
						+ "Stacktrace:"
						+ Environment.NewLine
						+ exc.StackTrace);
				}
			}
		}
	}
}
