using System;
using TACore;
using MonoMac.AppKit;

namespace TACmd {
	class MainClass {
		public static void Main(string[] args) {
			Console.WriteLine("Hello World!");

			WoWInstall install = new WoWInstall(Platform.CurrentPlatform.WoWInstallationDirectory);
				//("/Applications/World of Warcraft");

			Console.Out.WriteLine(install.CalculateHashForInstallFiles());

		}
	}
}
