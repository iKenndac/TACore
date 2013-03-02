using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using KNFoundation;

namespace TeleportAddons {

    public interface ISettingsFileProvider {
        
        string FullPathToFileContainer();
        List<SettingsFile> SettingsFiles { get; }
        SettingsFile AddSettingsFileWithRelativePathToContainer(string relativePath);

        void RemoveAllSideCarFiles();
        void GenerateSideCarFiles();

    }

    public class SettingsFile {

		static List<string> fileNameFragmentExclusionList;

        private static List<string> FileNameFragmentExclusionList() {

            if (fileNameFragmentExclusionList == null) {

                string listPath = KNBundle.MainBundle().PathForResourceOfType(Constants.kSettingsFileNameExclusionFragmentListFileName, "");

                try {
                    if (File.Exists(listPath)) {
                        Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(File.ReadAllBytes(listPath));
                        ArrayList list = (ArrayList)plist[Constants.kSettingsFileNameExclusionFragmentListKey];

                        if (list != null) {
                            fileNameFragmentExclusionList = new List<string>((string[])list.ToArray(typeof(string)));
                        }
                    }

                } catch (Exception) { }

            }
            return fileNameFragmentExclusionList;
        }




        private static List<string> SettingsFileNamesInDirectory(string directoryPath) {

            List<string> fileNames = new List<string>();

            if (Directory.Exists(directoryPath)) {
                DirectoryInfo info = new DirectoryInfo(directoryPath);
                foreach (FileInfo file in info.GetFiles()) {

                    Boolean shouldBeIncluded = true;

					if (FileNameFragmentExclusionList() != null) {
                    	foreach (string fragment in FileNameFragmentExclusionList()) {
                        	if (file.Name.IndexOf(fragment, StringComparison.CurrentCultureIgnoreCase) > -1) {
								shouldBeIncluded = false;
								break;
							}
						}
					}

                    if (shouldBeIncluded) {
                        fileNames.Add(file.Name);
                    }
                }
            }

            return fileNames;
        }

        public static List<SettingsFile> SettingsFilesInContainer(ISettingsFileProvider container) {

            List<SettingsFile> files = new List<SettingsFile>();

            string containerPath = container.FullPathToFileContainer();

            if (Directory.Exists(containerPath)) {

                foreach (string fileName in SettingsFileNamesInDirectory(containerPath)) {
                    files.Add(new SettingsFile(fileName, container));
                }

                string savedVariablesPath = Path.Combine(containerPath, Constants.kSavedVariablesFolderName);

                if (Directory.Exists(savedVariablesPath)) {

                    foreach (string fileName in SettingsFileNamesInDirectory(savedVariablesPath)) {
                        files.Add(new SettingsFile(Path.Combine(Constants.kSavedVariablesFolderName, fileName), container));
                    }
                }
            }

            return files;
        }

        public SettingsFile(string relativePathToContainer, ISettingsFileProvider container) {

            Parent = container;
            PathRelativeToParent = relativePathToContainer;

            ResetChecksum();

            if (!ApplySideCarFileAttributes()) {
                if (File.Exists(FullPath())) {
                    NormalisedUTCDateCreated = File.GetCreationTimeUtc(FullPath());
                    NormalisedUTCDateModified = File.GetLastWriteTimeUtc(FullPath());
                }
            }
        }


        private Boolean ApplySideCarFileAttributes() {

            string sideCarPath = String.Concat(FullPath(), Constants.kSideCarFileSuffix);

            if (File.Exists(sideCarPath)) {

                try {
                    Dictionary<string, object> sideCarAttributes = KNPropertyListSerialization.PropertyListWithData(File.ReadAllBytes(sideCarPath));

                    if (((string)sideCarAttributes[Constants.kSideCarChecksumKey]).Equals(Checksum, StringComparison.CurrentCultureIgnoreCase)) {
                    
                        NormalisedUTCDateCreated = (DateTime)sideCarAttributes[Constants.kSideCarDateCreatedUTCKey];
                        NormalisedUTCDateModified = (DateTime)sideCarAttributes[Constants.kSideCarDateModifiedUTCKey];

                        if (File.Exists(FullPath())) {
                            File.SetCreationTimeUtc(FullPath(), NormalisedUTCDateCreated);
                            File.SetLastWriteTimeUtc(FullPath(), NormalisedUTCDateModified);

                            return true;
                        }
                    }

                } catch (Exception) { }
            }

            return false;
        }

        public void RemoveSideCarFile() {

            string sideCarPath = String.Concat(FullPath(), Constants.kSideCarFileSuffix);

            try {
                if (File.Exists(sideCarPath)) {
                    File.Delete(sideCarPath);
                }
            } catch(Exception) {}
        }

        public void GenerateSideCarFile() {

            Dictionary<string, object> sideCar = new Dictionary<string, object>();
            sideCar.Add(Constants.kSideCarChecksumKey, Checksum);
            sideCar.Add(Constants.kSideCarDateCreatedUTCKey, NormalisedUTCDateCreated);
            sideCar.Add(Constants.kSideCarDateModifiedUTCKey, NormalisedUTCDateModified);

            try {
                byte[] plist = KNPropertyListSerialization.DataWithPropertyList(sideCar);

                File.WriteAllBytes(String.Concat(FullPath(), Constants.kSideCarFileSuffix), plist);
            } catch (Exception) {
                // If we can't create a new one, remove the old one
                RemoveSideCarFile();
            }
        }

        public string FullPath() {
            return Path.Combine(
                    Parent.FullPathToFileContainer(),
                    PathRelativeToParent
                );
        }

        public void ResetChecksum() {

            if (File.Exists(FullPath())) {

                byte[] bytes = File.ReadAllBytes(FullPath());

                if (bytes != null) {
                    MD5 md5 = MD5.Create();
                    byte[] hash = md5.ComputeHash(bytes);

                    // Build the final string by converting each byte
                    // into hex and appending it to a StringBuilder
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++) {
                        sb.Append(hash[i].ToString("X2"));
                    }

                    Checksum = sb.ToString();
                } else {
                    Checksum = "";
                }
            } else {
                Checksum = "";
            }
        }

        public void InheritMetaDataFromSettingsFile(SettingsFile anotherSettingsFile) {
            NormalisedUTCDateCreated = anotherSettingsFile.NormalisedUTCDateCreated;
            NormalisedUTCDateModified = anotherSettingsFile.NormalisedUTCDateModified;
        }

        #region Properties 

        public string PathRelativeToParent {
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

        public ISettingsFileProvider Parent {
            get;
            private set;
        }

        public string Checksum {
            get;
            private set;
        }

        #endregion

    }
}
