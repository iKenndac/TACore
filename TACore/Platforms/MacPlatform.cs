using System;
using MonoMac.ObjCRuntime;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.IO;

namespace TACore {
	public class MacPlatform : Platform {

		static MacPlatform() {
			NSApplication.Init();
		}

		public override string WoWInstallationDirectory {
			get {
				string wowPath = NSWorkspace.SharedWorkspace.AbsolutePathForAppBundle(Constants.kWorldOfWarcraftApplicationIdentifier);
				if (!String.IsNullOrWhiteSpace(wowPath))
					return Path.GetDirectoryName(wowPath);

				return null;
			}
		}

		public override bool WoWIsOpen {
			get {

				NSDictionary[] apps = NSWorkspace.SharedWorkspace.LaunchedApplications;
				NSString key = new NSString("NSApplicationBundleIdentifier");
				NSString wowIdentifier = new NSString(Constants.kWorldOfWarcraftApplicationIdentifier);

				foreach (NSDictionary dict in apps) {

					if (dict.ContainsKey(key)) {
						NSString value = (NSString)dict[key];
						if (value.Equals(wowIdentifier))
							return true;
					}
				}

				return false;
			}
		}

		public override string ApplicationDataDirectory {
			get {
				string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
				return Path.Combine(homeFolder, "Library", "Application Support");
			}
		}

	}
}

