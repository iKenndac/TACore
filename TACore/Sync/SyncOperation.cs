using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;
using SparkleDotNET;

namespace TeleportAddons {
    class SyncOperation {


        public SyncOperation(WoWInstall targetInstall, WoWInstall syncSource) {
            Target = targetInstall;
            Source = syncSource;
        }

        public SyncLog Sync() {

            SyncLog log = new SyncLog();
            log.StartDate = DateTime.Now;

            // Update source with new stuff in destination 

            SyncLogStep updateSourceFromTargetStep = new SyncLogStep();
            updateSourceFromTargetStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("updating source from target title", "");
            log.Steps.Add(updateSourceFromTargetStep);

            Boolean sourceWasChanged = UpdateInstallWithNewAndUpdatedFileFromSource(Source, Target, false, updateSourceFromTargetStep);

            // Reset both

            Target = new WoWInstall(Target.DirectoryPath);
            Source = new WoWInstall(Source.DirectoryPath);

            // Update Destination with new stuff in source

            SyncLogStep updateTargetFromSourceStep = new SyncLogStep();
            updateTargetFromSourceStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("updating target from source title", "");
            log.Steps.Add(updateTargetFromSourceStep);

            Boolean destinationWasChanged = UpdateInstallWithNewAndUpdatedFileFromSource(Target, Source, false, updateTargetFromSourceStep);

            // Reset both again 

            Target = new WoWInstall(Target.DirectoryPath);
            Source = new WoWInstall(Source.DirectoryPath);

            // Check consistency 

            SyncLogStep checkingConsistencyStep = new SyncLogStep();
            checkingConsistencyStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("checking consistency title", "");
            log.Steps.Add(checkingConsistencyStep);

            SyncLogStep checkingConsistencyFromTargetToSourceStep = new SyncLogStep();
            checkingConsistencyFromTargetToSourceStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("checking consistency from target to source title", "");
            checkingConsistencyStep.Children.Add(checkingConsistencyFromTargetToSourceStep);

            Boolean sourcesHaveDifferencesAfterSync = UpdateInstallWithNewAndUpdatedFileFromSource(Target, Source, true, checkingConsistencyFromTargetToSourceStep);

            if (sourcesHaveDifferencesAfterSync == true) {
                checkingConsistencyStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                checkingConsistencyFromTargetToSourceStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
            }

            SyncLogStep checkingConsistencyFromSourceToTargetStep = new SyncLogStep();
            checkingConsistencyFromSourceToTargetStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("checking consistency from source to target title", "");
            checkingConsistencyStep.Children.Add(checkingConsistencyFromSourceToTargetStep);

            sourcesHaveDifferencesAfterSync = UpdateInstallWithNewAndUpdatedFileFromSource(Source, Target, true, checkingConsistencyFromSourceToTargetStep);

            if (sourcesHaveDifferencesAfterSync == true) {
                checkingConsistencyFromSourceToTargetStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                checkingConsistencyStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
            }
        
            // Create sidecar files in sync source if consistent 

            SyncLogStep cleaningUpStep = new SyncLogStep();
            cleaningUpStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("cleaning up title", "");
            log.Steps.Add(cleaningUpStep);

            if (!sourcesHaveDifferencesAfterSync) {
                Source.GenerateSideCarFiles();
                Target.RemoveAllSideCarFiles();
            }

            // Done!

            SyncLog.SyncResult result = SyncLog.SyncResult.kNothingChanged;

            if (sourceWasChanged) {
                result = SyncLog.SyncResult.kSourceWasUpdated;
            }

            if (destinationWasChanged) {
                result = (result | SyncLog.SyncResult.kDestinationWasUpdated);
            }

            log.Result = result;
            log.EndDate = DateTime.Now;

            return log;
        }

        public Boolean PerformDryRunOfSourceToTargetSync() {
            SyncLogStep step = new SyncLogStep();
            return UpdateInstallWithNewAndUpdatedFileFromSource(Target, Source, true, step);
        }
        
        public Boolean PerformDryRunOfTargetToSourceSync() {
            SyncLogStep step = new SyncLogStep();
            return UpdateInstallWithNewAndUpdatedFileFromSource(Source, Target, true, step);
        }

        private Boolean UpdateInstallWithNewAndUpdatedFileFromSource(
            WoWInstall targetInstall, 
            WoWInstall sourceInstall,
            Boolean isTestRun,
            SyncLogStep parentSyncStep
            ) {

            Boolean targetInstallWasModified = false;

            // First, Addons!

            foreach (WoWAddon sourceAddon in sourceInstall.Addons) {

                SyncLogStep thisAddonStep = new SyncLogStep();
                thisAddonStep.StepDescription = String.Format(
                    KNBundleGlobalHelpers.KNLocalizedString("syncing addon formatter", ""),
                    sourceAddon.Name
                    );

                parentSyncStep.Children.Add(thisAddonStep);

                WoWAddon targetAddon = AddonEquivalentToAddonInAddons(sourceAddon, targetInstall.Addons);

                if (targetAddon == null) {

                    SyncLogStep targetAddonDoesntExistStep = new SyncLogStep();
                    targetAddonDoesntExistStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("addon doesn't exist in target title", "");
                    thisAddonStep.Children.Add(targetAddonDoesntExistStep);

                    targetInstallWasModified = true;

                    if (!isTestRun) {
                        try {
                            targetInstall.AddAddonCopyingFromAddonReplacingExistingAddon(sourceAddon, null);
                        } catch (Exception) {
                            targetAddonDoesntExistStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                            thisAddonStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                        }
                    }

                } else {

                    SyncLogStep targetAddonVersionStep = new SyncLogStep();
                    thisAddonStep.Children.Add(targetAddonVersionStep);

                    int result = SUStandardVersionComparator.SharedComparator().CompareVersionToVersion(sourceAddon.Version, targetAddon.Version);

                    if (result > 0) {
                        // Source is newer!

                        targetAddonVersionStep.StepDescription = String.Format(
                            KNBundleGlobalHelpers.KNLocalizedString("addon is newer in source title formatter", ""),
                            sourceAddon.Version,
                            targetAddon.Version
                            );

                        targetInstallWasModified = true;

                        if (!isTestRun) {
                            try {
                                targetInstall.AddAddonCopyingFromAddonReplacingExistingAddon(sourceAddon, targetAddon);
                            } catch (Exception) {
                                targetAddonVersionStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                                thisAddonStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                            }
                        }

                    } else if (result < 0) {
                        // Source is older

                        targetAddonVersionStep.StepDescription = String.Format(
                            KNBundleGlobalHelpers.KNLocalizedString("addon is older in source title formatter", ""),
                            sourceAddon.Version,
                            targetAddon.Version
                            );

                    } else {

                        // They have the same version

                        if (targetAddon.Checksum.Equals(sourceAddon.Checksum)) {
                            // Do nothing, they're the same
                            targetAddonVersionStep.StepDescription = String.Format(
                                KNBundleGlobalHelpers.KNLocalizedString("addon is same in source title formatter", ""),
                                sourceAddon.Version,
                                targetAddon.Version
                                );

                        } else if (targetAddon.NormalisedUTCDateModified.CompareTo(sourceAddon.NormalisedUTCDateModified) < 0) {

                            targetInstallWasModified = true;

                            targetAddonVersionStep.StepDescription = String.Format(
                                KNBundleGlobalHelpers.KNLocalizedString("addon with same version is newer in source title formatter", ""),
                                sourceAddon.Version,
                                targetAddon.Version
                                );

                            if (!isTestRun) {

                                try {
                                    targetInstall.AddAddonCopyingFromAddonReplacingExistingAddon(sourceAddon, targetAddon);
                                } catch (Exception) {
                                    targetAddonVersionStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                                    thisAddonStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                                }
                            }
                        } else {
                            // Do nothing, this will be caught in the other sweep
                            targetAddonVersionStep.StepDescription = String.Format(
                                KNBundleGlobalHelpers.KNLocalizedString("",""),
                                sourceAddon.Version,
                                targetAddon.Version
                                );
                        }
                    }
                }
            } // foreach Addons

            // Then settings

            foreach (WoWAccount sourceAccount in sourceInstall.Accounts) {

                SyncLogStep thisAccountStep = new SyncLogStep();
                thisAccountStep.StepDescription = String.Format(
                    KNBundleGlobalHelpers.KNLocalizedString("syncing account formatter", ""),
                    sourceAccount.AccountName
                    );
                parentSyncStep.Children.Add(thisAccountStep);

                WoWAccount targetAccount = AccountEquivalentToAccountInAccounts(sourceAccount, targetInstall.Accounts);

                if (targetAccount == null) {

                    SyncLogStep targetAccountDoesntExistStep = new SyncLogStep();
                    targetAccountDoesntExistStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("account doesn't exist in target title", "");
                    thisAccountStep.Children.Add(targetAccountDoesntExistStep);

                    targetInstallWasModified = true;
                    targetAccount = targetInstall.AddAccountWithName(sourceAccount.AccountName);
                }

                SyncLogStep syncingAccountLevelSettingsStep = new SyncLogStep();
                syncingAccountLevelSettingsStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("syncing account level settings title", "");
                thisAccountStep.Children.Add(syncingAccountLevelSettingsStep);

                if (UpdateSettingsFileProviderWithNewAndUpdateFilesFromSourceProvider(targetAccount, sourceAccount, isTestRun, syncingAccountLevelSettingsStep)) {
                    targetInstallWasModified = true;
                }

                foreach (WoWRealm sourceRealm in sourceAccount.Realms) {

                    SyncLogStep thisRealmStep = new SyncLogStep();
                    thisRealmStep.StepDescription = String.Format(
                        KNBundleGlobalHelpers.KNLocalizedString("syncing realm formatter", ""),
                        sourceRealm.RealmName
                        );

                    thisAccountStep.Children.Add(thisRealmStep);

                    WoWRealm targetRealm = RealmEquivalentToRealmInRealms(sourceRealm, targetAccount.Realms);

                    if (targetRealm == null) {

                        SyncLogStep targetRealmDoesntExistStep = new SyncLogStep();
                        targetRealmDoesntExistStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("realm doesn't exist in target title", "");
                        thisRealmStep.Children.Add(targetRealmDoesntExistStep);

                        targetInstallWasModified = true;
                        targetRealm = targetAccount.AddRealmWithName(sourceRealm.RealmName);
                    }

                    SyncLogStep syncingRealmLevelSettingsStep = new SyncLogStep();
                    syncingRealmLevelSettingsStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("syncing realm level settings title", "");
                    thisRealmStep.Children.Add(syncingRealmLevelSettingsStep);

                    if (UpdateSettingsFileProviderWithNewAndUpdateFilesFromSourceProvider(targetRealm, sourceRealm, isTestRun, syncingRealmLevelSettingsStep)) {
                        targetInstallWasModified = true;
                    }

                    foreach (WoWCharacter sourceCharacter in sourceRealm.Characters) {

                        SyncLogStep thisCharacterStep = new SyncLogStep();
                        thisCharacterStep.StepDescription = String.Format(
                            KNBundleGlobalHelpers.KNLocalizedString("syncing character formatter", ""),
                            sourceCharacter.CharacterName
                            );
                        thisRealmStep.Children.Add(thisCharacterStep);

                        WoWCharacter targetCharacter = CharacterEquivalentToCharacterInCharacters(sourceCharacter, targetRealm.Characters);

                        if (targetCharacter == null) {

                            SyncLogStep targetCharacterDoesntExistStep = new SyncLogStep();
                            targetCharacterDoesntExistStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("character doesn't exist in target title", "");
                            thisCharacterStep.Children.Add(targetCharacterDoesntExistStep);

                            targetInstallWasModified = true;
                            targetCharacter = targetRealm.AddCharacterWithName(sourceCharacter.CharacterName);
                        }

                        SyncLogStep syncingCharacterLevelSettingsStep = new SyncLogStep();
                        syncingCharacterLevelSettingsStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("syncing character level settings title", "");
                        thisCharacterStep.Children.Add(syncingCharacterLevelSettingsStep);

                        if (UpdateSettingsFileProviderWithNewAndUpdateFilesFromSourceProvider(targetCharacter, sourceCharacter, isTestRun, syncingCharacterLevelSettingsStep)) {
                            targetInstallWasModified = true;
                        }
                    }
                }
            }

            return targetInstallWasModified;
        }

        private Boolean UpdateSettingsFileProviderWithNewAndUpdateFilesFromSourceProvider(
            ISettingsFileProvider targetProvider,
            ISettingsFileProvider sourceProvider,
            Boolean isTestRun,
            SyncLogStep parentSyncStep
            ) {

            Boolean targetProviderWasModified = false;

            foreach (SettingsFile sourceFile in sourceProvider.SettingsFiles) {

                SyncLogStep thisSourceFileStep = new SyncLogStep();
                thisSourceFileStep.StepDescription = String.Format(
                    KNBundleGlobalHelpers.KNLocalizedString("syncing settings file formatter", ""),
                    sourceFile.PathRelativeToParent
                    );

                parentSyncStep.Children.Add(thisSourceFileStep);

                SettingsFile targetFile = SettingsFileEquivalentToSettingsFileInSettingsFiles(sourceFile, targetProvider.SettingsFiles);

                if (targetFile == null) {

                    SyncLogStep targetFileDoesntExistStep = new SyncLogStep();
                    targetFileDoesntExistStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("settings file doesnt exist in target title", "");
                    thisSourceFileStep.Children.Add(targetFileDoesntExistStep);

                    targetProviderWasModified = true;

                    targetFile = targetProvider.AddSettingsFileWithRelativePathToContainer(sourceFile.PathRelativeToParent);

                    if (!isTestRun) {

                        SyncLogStep copyFileStep = CopySettingsFileToSettingsFile(sourceFile, targetFile);
                        thisSourceFileStep.Children.AddRange(copyFileStep.Children);
                        thisSourceFileStep.Status = copyFileStep.Status;
                    }

                } else {

                    if (targetFile.Checksum.Equals(sourceFile.Checksum)) {

                        SyncLogStep filesAreIdenticalStep = new SyncLogStep();
                        filesAreIdenticalStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("settings files are identical title", "");
                        thisSourceFileStep.Children.Add(filesAreIdenticalStep);

                    } else if (targetFile.NormalisedUTCDateModified.CompareTo(sourceFile.NormalisedUTCDateModified) < 0) {

                        SyncLogStep fileExistsStep = new SyncLogStep();
                        fileExistsStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("settings file exists in target title", "");
                        thisSourceFileStep.Children.Add(fileExistsStep);

                        targetProviderWasModified = true;

                        if (!isTestRun) {
                            SyncLogStep copyFileStep = CopySettingsFileToSettingsFile(sourceFile, targetFile);
                            thisSourceFileStep.Children.AddRange(copyFileStep.Children);
                            thisSourceFileStep.Status = copyFileStep.Status;
                        }

                    } else {
                        // Do nothing, the target file is newer, and will be caught in the other sweep

                        SyncLogStep destinationIsNewerStep = new SyncLogStep();
                        destinationIsNewerStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("target file is newer than source title", "");
                        thisSourceFileStep.Children.Add(destinationIsNewerStep);
                    }
                }
            }

            return targetProviderWasModified;
        }

        private SyncLogStep CopySettingsFileToSettingsFile(SettingsFile sourceFile, SettingsFile targetFile) {

            SyncLogStep step = new SyncLogStep();

            string targetDirectory = Path.GetDirectoryName(targetFile.FullPath());

            if (!Directory.Exists(targetDirectory)) {

                SyncLogStep creatingDirectoryStep = new SyncLogStep();
                creatingDirectoryStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("creating missing directory title", "");
                step.Children.Add(creatingDirectoryStep);

                try {
                    Directory.CreateDirectory(targetDirectory);
                } catch (Exception ex) {

                    SyncLogStep creatingDirectoryFailedStep = new SyncLogStep();
                    creatingDirectoryFailedStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    creatingDirectoryFailedStep.StepDescription = String.Format(
                        KNBundleGlobalHelpers.KNLocalizedString("failed with error formatter", ""),
                        ex.Message
                        );

                    creatingDirectoryStep.Children.Add(creatingDirectoryFailedStep);
                    creatingDirectoryStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    step.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    return step;
                }
            }

            // We have to delete old files first

            if (File.Exists(targetFile.FullPath())) {

                SyncLogStep removingExistingFileStep = new SyncLogStep();
                removingExistingFileStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("removing existing file title", "");
                step.Children.Add(removingExistingFileStep);

                try {
                    File.Delete(targetFile.FullPath());
                } catch (Exception ex) {

                    SyncLogStep removingFileFailedStep = new SyncLogStep();
                    removingFileFailedStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    removingFileFailedStep.StepDescription = String.Format(
                        KNBundleGlobalHelpers.KNLocalizedString("failed with error formatter", ""),
                        ex.Message
                        );

                    removingExistingFileStep.Children.Add(removingFileFailedStep);
                    removingExistingFileStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    step.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                    return step;
                }
            }

            SyncLogStep copyingFileStep = new SyncLogStep();
            copyingFileStep.StepDescription = KNBundleGlobalHelpers.KNLocalizedString("copying file title", "");
            step.Children.Add(copyingFileStep);

            try {
                File.Copy(sourceFile.FullPath(), targetFile.FullPath());
            } catch (Exception ex) {

                SyncLogStep copyingFileFailedStep = new SyncLogStep();
                copyingFileFailedStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                copyingFileFailedStep.StepDescription = String.Format(
                    KNBundleGlobalHelpers.KNLocalizedString("failed with error formatter", ""),
                    ex.Message
                    );

                copyingFileStep.Children.Add(copyingFileFailedStep);
                copyingFileStep.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                step.Status = SyncLogStep.SyncLogStepStatus.kStepStatusFailed;
                return step;
            }

            targetFile.ResetChecksum();
            targetFile.InheritMetaDataFromSettingsFile(sourceFile);
            
            return step;
        }

        #region Finding Stuff

        private WoWAddon AddonEquivalentToAddonInAddons(WoWAddon sourceAddon, List<WoWAddon> listOfAddons) {

            if (sourceAddon == null || listOfAddons == null) return null;

            foreach (WoWAddon potentialAddon in listOfAddons) {
                if (sourceAddon.DirectoryName.Equals(potentialAddon.DirectoryName)) {
                    return potentialAddon;
                }
            }
            return null;
        }

        private SettingsFile SettingsFileEquivalentToSettingsFileInSettingsFiles(SettingsFile sourceFile, List<SettingsFile> listOfFiles) {

            if (sourceFile == null || listOfFiles == null) return null;

            foreach (SettingsFile potentialFile in listOfFiles) {
                if (sourceFile.PathRelativeToParent.Equals(potentialFile.PathRelativeToParent)) {
                    return potentialFile;
                }
            }
            return null;
        }

        private WoWAccount AccountEquivalentToAccountInAccounts(WoWAccount sourceAccount, List<WoWAccount> listOfAccounts) {
            
            if (sourceAccount == null || listOfAccounts == null) return null;

            foreach (WoWAccount potentialAccount in listOfAccounts) {
                if (potentialAccount.AccountName.Equals(sourceAccount.AccountName)) {
                    return potentialAccount;
                }
            }
            return null;
        }

        private WoWRealm RealmEquivalentToRealmInRealms(WoWRealm sourceRealm, List<WoWRealm> listOfRealms) {

            if (sourceRealm == null || listOfRealms == null) return null;

            foreach (WoWRealm potentialRealm in listOfRealms) {
                if (potentialRealm.RealmName.Equals(sourceRealm.RealmName)) {
                    return potentialRealm;
                }
            }
            return null;
        }

        private WoWCharacter CharacterEquivalentToCharacterInCharacters(WoWCharacter sourceCharacter, List<WoWCharacter> listOfCharacters) {

            if (sourceCharacter == null || listOfCharacters == null) return null;

            foreach (WoWCharacter potentialCharacter in listOfCharacters) {
                if (potentialCharacter.CharacterName.Equals(sourceCharacter.CharacterName)) {
                    return potentialCharacter;
                }
            }
            return null;
        }

        #endregion

        #region Properties

        public WoWInstall Target {
            get;
            private set;
        }

        public WoWInstall Source {
            get;
            private set;
        }

        #endregion
    }
}
