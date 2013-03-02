using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;

namespace TACore {
    public class WoWAddon {

        static List<string> addonFileNameFragmentExclusionList;

        private static List<string> FileNameFragmentExclusionList() {

            if (addonFileNameFragmentExclusionList == null) {

                string listPath = KNBundle.MainBundle().PathForResourceOfType(Constants.kAddonsFileNameExclusionFragmentListFileName, "");

                try {
                    if (File.Exists(listPath)) {
                        Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(File.ReadAllBytes(listPath));
                        ArrayList list = (ArrayList)plist[Constants.kAddonsFileNameExclusionFragmentListKey];

                        if (list != null) {
                            addonFileNameFragmentExclusionList = new List<string>((string[])list.ToArray(typeof(string)));
                        }
                    }

                } catch (Exception) { }

            }
            return addonFileNameFragmentExclusionList;
        }

        public static List<WoWAddon> AddOnsInInstall(WoWInstall install) {

            string addonsFolder = Path.Combine(
                install.DirectoryPath,
                Constants.kWoWInterfaceFolderName,
                Constants.kWoWInterfaceAddonsSubFolderName
                );

            List<WoWAddon> addons = new List<WoWAddon>();

            if (Directory.Exists(addonsFolder)) {

				foreach (DirectoryInfo potentialAddon in (new DirectoryInfo(addonsFolder).GetDirectories())) {

					bool shouldBeIncluded = true;

					if (FileNameFragmentExclusionList() != null) {
						foreach (string fragment in FileNameFragmentExclusionList()) {
							if (potentialAddon.Name.IndexOf(fragment, StringComparison.CurrentCultureIgnoreCase) > -1) {
								shouldBeIncluded = false;
								break;
							}
						}
					}

					if (shouldBeIncluded) {
						WoWAddon addon = new WoWAddon(install, potentialAddon.Name);
						if (addon.ValidAddon) {
							addons.Add(addon);
						}
					}
				}
			}

            return addons;
        }

        public WoWAddon(WoWInstall install, string addonDirectoryName) {
            Install = install;
            DirectoryName = addonDirectoryName;
            
            if (Directory.Exists(FullPathToAddon())) {

                ValidAddon = ScanDetails();
                Checksum = new DirectoryInfo(FullPathToAddon()).MD5Hash();

                if (!ApplySideCarFileAttributes()) {

                    // Find the newest file in the whole addon

                    DateTime newestModificationDate;
                    DateTime newestCreationDate;

                    new DirectoryInfo(FullPathToAddon()).NewestFileDatesUTC(out newestModificationDate, out newestCreationDate);

                    NormalisedUTCDateCreated = newestCreationDate;
                    NormalisedUTCDateModified = newestModificationDate;
                }
            }
             
        }

        

        public void GenerateSideCarFile() {

            string sidecarPath = String.Concat(FullPathToAddon(), Constants.kSideCarFileSuffix);

            Dictionary<string, object> plist = new Dictionary<string, object>();

            plist.Add(Constants.kSideCarChecksumKey, Checksum);
            plist.Add(Constants.kSideCarDateCreatedUTCKey, NormalisedUTCDateCreated);
            plist.Add(Constants.kSideCarDateModifiedUTCKey, NormalisedUTCDateModified);

            byte[] plistData = KNPropertyListSerialization.DataWithPropertyList(plist);

            if (plistData != null) {
                File.WriteAllBytes(sidecarPath, plistData);
            }

        }

        public void RemoveSideCarFile() {

            string sidecarPath = String.Concat(FullPathToAddon(), Constants.kSideCarFileSuffix);

            if (File.Exists(sidecarPath)) {
                try {
                    File.Delete(sidecarPath);
                } catch (Exception) {
                }
            }
        }

        private Boolean ApplySideCarFileAttributes() {

            string sidecarPath = String.Concat(FullPathToAddon(), Constants.kSideCarFileSuffix);

            if (File.Exists(sidecarPath)) {

                try {

                    byte[] plistData = File.ReadAllBytes(sidecarPath);
                    Dictionary<string, object> sideCarAttributes = KNPropertyListSerialization.PropertyListWithData(plistData);

                    if (sideCarAttributes[Constants.kSideCarChecksumKey].Equals(Checksum)) {

                        NormalisedUTCDateCreated = (DateTime)sideCarAttributes[Constants.kSideCarDateCreatedUTCKey];
                        NormalisedUTCDateModified = (DateTime)sideCarAttributes[Constants.kSideCarDateModifiedUTCKey];

                        // Directory and TOC

                        string addonPath = FullPathToAddon();

                        if (Directory.Exists(addonPath)) {
                            Directory.SetCreationTimeUtc(addonPath, NormalisedUTCDateCreated);
                            Directory.SetLastWriteTimeUtc(addonPath, NormalisedUTCDateModified);
                        }

                        string tocPath = Path.Combine(
                            FullPathToAddon(),
                            String.Concat(DirectoryName, ".", Constants.kWoWAddonTOCExtension)
                            );

                        if (File.Exists(tocPath)) {
                            File.SetCreationTimeUtc(tocPath, NormalisedUTCDateCreated);
                            File.SetLastWriteTimeUtc(tocPath, NormalisedUTCDateModified);
                        }

                        return true;

                    }

                } catch (Exception) {

                }
            }
            return false;
        }

        public string FullPathToAddon() {
            return Path.Combine(
                Install.DirectoryPath,
                Constants.kWoWInterfaceFolderName,
                Constants.kWoWInterfaceAddonsSubFolderName,
                DirectoryName
                );
        }

        #region AddOn Parsing

        private Boolean ScanDetails() {

            string tocPath = Path.Combine(
                            FullPathToAddon(),
                            String.Concat(DirectoryName, ".", Constants.kWoWAddonTOCExtension)
                            );

            if (File.Exists(tocPath)) {

                try {

                    string tocStr = File.ReadAllText(tocPath, Encoding.UTF8);

                    if (tocStr.Length > 0) {

                        Boolean titleFound = false;
                        Boolean versionFound = false;
                        Boolean curseVersionFound = false;

                        foreach (string tocLine in tocStr.Split(new char[] { '\n' })) {

                            if (!versionFound) {

                                int location = tocLine.IndexOf(Constants.kWoWTOCVersionPrefix, StringComparison.CurrentCultureIgnoreCase);
                                
                                if (location > -1) {

                                    string stringWithoutPrefix = tocLine.Substring(location + Constants.kWoWTOCVersionPrefix.Length);
                                    int colonLocation = stringWithoutPrefix.IndexOf(":");

                                    if (colonLocation > -1) {

                                        Version = StringByRemovingTOCEscapeSequencesFromString(stringWithoutPrefix.Substring(colonLocation + 1).Trim());
                                        versionFound = true;
                                    }
                                }
                            }

                            if (!versionFound && !curseVersionFound) {

                                int location = tocLine.IndexOf(Constants.kWoWTOCCurseVersionPrefix, StringComparison.CurrentCultureIgnoreCase);

                                if (location > -1) {

                                    string stringWithoutPrefix = tocLine.Substring(location + Constants.kWoWTOCCurseVersionPrefix.Length);
                                    int colonLocation = stringWithoutPrefix.IndexOf(":");

                                    if (colonLocation > -1) {

                                        Version = StringByRemovingTOCEscapeSequencesFromString(stringWithoutPrefix.Substring(colonLocation + 1).Trim());
                                        curseVersionFound = true;
                                    }
                                }
                            }

                            if (!titleFound) {

                                int location = tocLine.IndexOf(Constants.kWoWTOCTitlePrefix, StringComparison.CurrentCultureIgnoreCase);

                                if (location > -1) {

                                    string stringWithoutPrefix = tocLine.Substring(location + Constants.kWoWTOCTitlePrefix.Length);
                                    int colonLocation = stringWithoutPrefix.IndexOf(":");

                                    if (colonLocation > -1) {

                                        Name = StringByRemovingTOCEscapeSequencesFromString(stringWithoutPrefix.Substring(colonLocation + 1).Trim());
                                        titleFound = true;
                                    }
                                }
                            }
                        }

                        if (!versionFound && !curseVersionFound) {
                            Version = Constants.kAddonMissingVersionSubstitutedVersion;
                        }

                        if (Version.Length > 1) {
                            Name = StringByRemovingFragmentsOfVersionStringFromString(Version, Name);
                        }

                        return (titleFound);

                    }

                } catch (Exception) {
                    throw;
                }
            }

            return false;
        }

        private string StringByRemovingFragmentsOfVersionStringFromString(string versionString, string anotherString) {

            if (anotherString == null) {
                return null;
            }

            if (versionString == null) {
                return anotherString;
            }

            string cleanedString = anotherString.Replace(versionString, "");
            string[] fragments = versionString.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string fragment in fragments) {
                if (fragment.Length > 1) {
                    // Don't remove dashes, commas, etc
                    cleanedString = cleanedString.Replace(fragment, "");
                }
            }
            return cleanedString.Trim();
        }

        private string StringByRemovingTOCEscapeSequencesFromString(string str) {

            if (str == null) {
                return null;
            }

            string cleanedStr = str.Replace("|r", "");

            // Clear out colours, which is more complex.

            Boolean clearOfColours = false;

            while (!clearOfColours) {

                int locationOfColourTag = cleanedStr.IndexOf("|c");

                if (locationOfColourTag == -1) {
                    clearOfColours = true;
                } else {
                    int kColourTagLength = 10; // i.e. |cFF002255, see TOC string markup reference

                    if ((locationOfColourTag + kColourTagLength) > cleanedStr.Length) {
                        // ?!
                        cleanedStr = cleanedStr.Replace("|c", "");
                    } else {
                        cleanedStr = cleanedStr.Remove(locationOfColourTag, kColourTagLength);
                    }
                }
            }

            return cleanedStr;
        }

        #endregion



        #region Properties

        public string DirectoryName {
            get;
            private set;
        }

        public string Name {
            get;
            private set;
        }

        public string Version {
            get;
            private set;
        }

        public WoWInstall Install {
            get;
            private set;
        }

        public DateTime NormalisedUTCDateModified {
            get;
            private set;
        }

        public DateTime NormalisedUTCDateCreated {
            get;
            private set;
        }

        public string Checksum {
            get;
            private set;
        }

        public Boolean ValidAddon {
            get;
            private set;
        }

        #endregion

    }
}
