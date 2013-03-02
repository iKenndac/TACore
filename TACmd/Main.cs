using System;
using TACore;

namespace TACmd {
	class MainClass {
		public static void Main(string[] args) {
			Console.WriteLine("Hello World!");

			WoWInstall install = new WoWInstall("/Applications/World of Warcraft");

			Console.Out.WriteLine(install.CalculateHashForInstallFiles());

		}
	}
}
