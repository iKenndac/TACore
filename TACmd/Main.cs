using System;
using TACore;
using System.Threading;
using MonoMac.AppKit;

namespace TACmd {
	class MainClass {

		static ManualResetEvent reset = new ManualResetEvent(false);

		public static void Main(string[] args) {

			WoWInstall install = new WoWInstall(Platform.CurrentPlatform.WoWInstallationDirectory);
				//("/Applications/World of Warcraft");

			SyncSource source = new FileSystemSyncSource("/Users/dkennett/Desktop/Test");

			SyncContainer container = new SyncContainer(source, install);

			container.SyncFailed += delegate(SyncContainer sender, Exception exception) {
				Console.Out.WriteLine("Setup failed: {0}", exception);
			};

			container.SyncStarting += delegate(SyncContainer sender) {
				Console.Out.WriteLine("Starting...");
			};

			container.SyncSucceeded += delegate(SyncContainer sender, SyncLog log) {
				LogController.SharedInstance().AddLog(log);
				Console.Out.WriteLine("Done!");
				reset.Set();
			};

			container.StartSync();

			reset.WaitOne();
			
		}
	}
}
