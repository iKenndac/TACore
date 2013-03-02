using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Permissions;

namespace TACore {
    public class WoWInstall {

        public WoWInstall(string path) {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                DirectoryPath = path.Substring(0, path.Length - 1);
            } else {
                DirectoryPath = path;
            }
            Addons = WoWAddon.AddOnsInInstall(this);
            Accounts = WoWAccount.AccountsInInstall(this);
        }

        public string DisplayName() {
            return Path.GetFileName(DirectoryPath);
        }

        public string IconName() {
			return "WoW.png";
        }

        public Boolean CanBeUsedAsResetSource() {

            int stuffCount = 0;

            if (Addons != null) {
                stuffCount += Addons.Count;
            }

            if (Accounts != null) {
                stuffCount += Accounts.Count;
            }

            return (stuffCount > 0);
        }

        public void GenerateSideCarFiles() {

            if (Addons != null) {
                foreach (WoWAddon addon in Addons) {
                    addon.GenerateSideCarFile();
                }
            }

            if (Accounts != null) {
                foreach (WoWAccount account in Accounts) {
                    account.GenerateSideCarFiles();
                }
            }
        }

        public void RemoveAllSideCarFiles() {

            if (Addons != null) {
                foreach (WoWAddon addon in Addons) {
                    addon.RemoveSideCarFile();
                }
            }

            if (Accounts != null) {
                foreach (WoWAccount account in Accounts) {
                    account.RemoveAllSideCarFiles();
                }
            }
        }

        public WoWInstall InstallByDuplicatingInstallToDirectory(string newInstallPath) {

            string sourceInterfaceFolder = Path.Combine(DirectoryPath, Constants.kWoWInterfaceFolderName);
            string sourceWTFAccountsFolder = Path.Combine(
                DirectoryPath, 
                Constants.kWoWWTFFolderName,
                Constants.kWoWWTFAccountSubFolderName
                );

            string destinationIntermediateInterfaceFolder = Path.Combine(newInstallPath, String.Concat(Constants.kWoWInterfaceFolderName, "-incoming"));
            string destinationIntermediateWTFAccountsFolder = Path.Combine(
                newInstallPath, 
                Constants.kWoWWTFFolderName,
                String.Concat(Constants.kWoWWTFAccountSubFolderName, "-incoming")
                );

            string destinationOutgoingInterfaceFolder = Path.Combine(newInstallPath, String.Concat(Constants.kWoWInterfaceFolderName, "-outgoing"));
            string destinationOutgoingWTFAccountsFolder = Path.Combine(
                newInstallPath,
                Constants.kWoWWTFFolderName,
                String.Concat(Constants.kWoWWTFAccountSubFolderName, "-outgoing")
                );

            string destinationInterfaceFolder = Path.Combine(newInstallPath, Constants.kWoWInterfaceFolderName);
            string destinationWTFAccountsFolder = Path.Combine(newInstallPath,
                Constants.kWoWWTFFolderName,
                Constants.kWoWWTFAccountSubFolderName
            );

            // Create target if it doesn't exist 

            if (!Directory.Exists(newInstallPath)) {
                Directory.CreateDirectory(newInstallPath);              
            }

            string wtfFolderPath = Path.Combine(newInstallPath, Constants.kWoWWTFFolderName);
            if (!Directory.Exists(wtfFolderPath)) {
                Directory.CreateDirectory(wtfFolderPath);
            }

            // First! Copy new files to intermediate places

            if (Directory.Exists(destinationIntermediateInterfaceFolder)) {
                Directory.Delete(destinationIntermediateInterfaceFolder, true);
            }

            if (Directory.Exists(destinationIntermediateWTFAccountsFolder)) {
                Directory.Delete(destinationIntermediateWTFAccountsFolder, true);
            }

            if (Directory.Exists(sourceInterfaceFolder)) {
                new DirectoryInfo(sourceInterfaceFolder).CopyTo(destinationIntermediateInterfaceFolder);
            }

            if (Directory.Exists(sourceWTFAccountsFolder)) {
                new DirectoryInfo(sourceWTFAccountsFolder).CopyTo(destinationIntermediateWTFAccountsFolder);
            }

            // Files are side-by-side. Rename old files.

            if (Directory.Exists(destinationOutgoingInterfaceFolder)) {
                Directory.Delete(destinationOutgoingInterfaceFolder);
            }

            DirectoryInfo destinationInterfaceInfo = new DirectoryInfo(destinationInterfaceFolder);

            if (destinationInterfaceInfo.Exists) {
                destinationInterfaceInfo.TryToMoveTo(destinationOutgoingInterfaceFolder);
            }

            if (Directory.Exists(destinationOutgoingWTFAccountsFolder)) {
                Directory.Delete(destinationOutgoingWTFAccountsFolder);
            }

            DirectoryInfo destinationWTFAccountsInfo = new DirectoryInfo(destinationWTFAccountsFolder);

            if (destinationWTFAccountsInfo.Exists) {
                destinationWTFAccountsInfo.TryToMoveTo(destinationOutgoingWTFAccountsFolder);
            }

            // Rename incoming files

            DirectoryInfo destinationIntermediateInterfaceInfo = new DirectoryInfo(destinationIntermediateInterfaceFolder);

            if (destinationIntermediateInterfaceInfo.Exists) {

                try {
                    destinationIntermediateInterfaceInfo.TryToMoveTo(destinationInterfaceFolder);

                } catch (Exception) {
                    
                    // Put outgoing files back 

                    if (Directory.Exists(destinationOutgoingInterfaceFolder)) {
                        
                        Directory.Move(destinationOutgoingInterfaceFolder, destinationInterfaceFolder);
                    }

                    if (Directory.Exists(destinationOutgoingWTFAccountsFolder)) {
                        Directory.Move(destinationOutgoingWTFAccountsFolder, destinationWTFAccountsFolder);
                    }

                    throw;
                }

            }

            DirectoryInfo destinationIntermediateWTFAccountsInfo = new DirectoryInfo(destinationIntermediateWTFAccountsFolder);

            if (destinationIntermediateWTFAccountsInfo.Exists) {

                try {
                    destinationIntermediateWTFAccountsInfo.TryToMoveTo(destinationWTFAccountsFolder);
                } catch (Exception) {

                    // Reset incoming Interface folder

                    Directory.Move(destinationInterfaceFolder, destinationIntermediateInterfaceFolder);

                    // Put outgoing files back 

                    if (Directory.Exists(destinationOutgoingInterfaceFolder)) {
                        Directory.Move(destinationOutgoingInterfaceFolder, destinationInterfaceFolder);
                    }

                    if (Directory.Exists(destinationOutgoingWTFAccountsFolder)) {
                        Directory.Move(destinationOutgoingWTFAccountsFolder, destinationWTFAccountsFolder);
                    }

                    throw;
                }
            }

            // Finally, delete outgoing

            if (Directory.Exists(destinationOutgoingInterfaceFolder)) {
                Directory.Delete(destinationOutgoingInterfaceFolder, true);
            }

            if (Directory.Exists(destinationOutgoingWTFAccountsFolder)) {
                Directory.Delete(destinationOutgoingWTFAccountsFolder, true);
            }

            return new WoWInstall(newInstallPath);
            
        }

        public WoWAccount AddAccountWithName(string accountName) {
            WoWAccount account = new WoWAccount(this, accountName);
            Accounts.Add(account);
            return account;
        }

        public void AddAddonCopyingFromAddonReplacingExistingAddon(WoWAddon anotherAddon, WoWAddon addonToReplace) {

            string targetDirectory = Path.Combine(
                DirectoryPath,
                Constants.kWoWInterfaceFolderName,
                Constants.kWoWInterfaceAddonsSubFolderName,
                anotherAddon.DirectoryName
                );

            string addOnsDirectory = Path.Combine(
                DirectoryPath,
                Constants.kWoWInterfaceFolderName,
                Constants.kWoWInterfaceAddonsSubFolderName
                );

            if (!Directory.Exists(addOnsDirectory)) {
                Directory.CreateDirectory(addOnsDirectory);
            }

            if (Directory.Exists(targetDirectory)) {
                Directory.Delete(targetDirectory);
            }

            new DirectoryInfo(anotherAddon.FullPathToAddon()).CopyTo(targetDirectory);

            WoWAddon newAddon = new WoWAddon(this, anotherAddon.DirectoryName);

            List<WoWAddon> addons = Addons;
            if (addonToReplace != null) {
                addons.Remove(addonToReplace);
            }

            addons.Add(newAddon);

            Addons = addons;
        }

        public string CalculateHashForInstallFiles() {

            // Algorithm:
            // 1) Hash every subfile of the interface directory.
            // 2) Sort hashes alphabetically 
            // 3) Concat that lot together, calculate hash of the hashes.
            // 4) Repeat 1-3 for WTF directory 
            // 5) return String.Concat([hash from step 3], [hash from step 4]

            List<string> hashes = new List<string>();

            string interfacePath = Path.Combine(DirectoryPath, Constants.kWoWInterfaceFolderName);

            if (Directory.Exists(interfacePath)) {
                string hash = new DirectoryInfo(interfacePath).MD5Hash();
                hashes.Add(hash);
            }

            string wtfPath = Path.Combine(DirectoryPath, Constants.kWoWWTFFolderName, Constants.kWoWWTFAccountSubFolderName);

            if (Directory.Exists(wtfPath)) {
                string hash = new DirectoryInfo(wtfPath).MD5Hash();
                hashes.Add(hash);
            }

            if (hashes.Count == 0) {
                return null;
            } else {
                return String.Concat(hashes.ToArray());
            }

        }

        #region Properties

        public string Version {
            get;
            private set;
        }

        public string DirectoryPath {
            get;
            private set;
        }

        public List<WoWAddon> Addons {
            get;
            private set;
        }

        public List<WoWAccount> Accounts {
            get;
            private set;
        }

        #endregion

    }
}
