using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using KNFoundation;

namespace TACore {

    interface SyncContainerDelegate {

        void SyncWillStart(SyncContainer aSyncContainer);
        void SyncDidFailOutsideOfSyncWithException(SyncContainer aSyncContainer, Exception exception);
        void SyncDidFailWithLog(SyncContainer aSyncContainer, SyncLog aLog);
        void SyncDidSucceedWithLog(SyncContainer aSyncContainer, SyncLog aLog);
        void SyncDidCancel(SyncContainer aSyncContainer);
    }

    class SyncContainer {

        BackgroundWorker syncOperationWorker;

        public SyncContainer(SyncSource syncSource, WoWInstall targetInstall) {
            Source = syncSource;
            Target = targetInstall;
        }

        public void StartSync() {

            if (ResetFromTargetInstall && ResetFromSyncSource) {
                FailWithException(new Exception(KNBundleGlobalHelpers.KNLocalizedString("can't reset from both sync sources error title", "")));
                return;
            }

            if (!IsSyncing && Source != null && Target != null) {
                if (Delegate != null) {
                    Delegate.SyncWillStart(this);
                }

				// MIGRATION: This used to will/didChangeValueForKey on CanCancel

                syncOperationWorker = new BackgroundWorker();
                syncOperationWorker.WorkerSupportsCancellation = true;

                syncOperationWorker.DoWork += DoSync;
                syncOperationWorker.RunWorkerCompleted += SyncCompleted;

                syncOperationWorker.RunWorkerAsync();

				// ---- didChange here
            }
        }

        public void Cancel() {
			// MIGRATION: This used to will/didChangeValueForKey on CanCancel
			if (syncOperationWorker != null && syncOperationWorker.WorkerSupportsCancellation) {
                syncOperationWorker.CancelAsync();
            }
			// ---- didChange here
       	}

        public Boolean CanCancel() {
            return (syncOperationWorker != null && syncOperationWorker.WorkerSupportsCancellation && (!syncOperationWorker.CancellationPending));
        }

        private void FailWithException(Exception ex) {
            if (Delegate != null) {
                Delegate.SyncDidFailOutsideOfSyncWithException(this, ex);
            }
        }

        private void SucceedWithLog(SyncLog log) {
            if (Delegate != null) {
                Delegate.SyncDidSucceedWithLog(this, log);
            }
        }

        private void InformDelegateOfCancellation() {
            if (Delegate != null) {
                Delegate.SyncDidCancel(this);
            }
        }


        private void DoSync(object sender, DoWorkEventArgs e) {

            BackgroundWorker backgroundWorker = (BackgroundWorker)sender;
           
            // Check if target install is suitable for reset if needed

            if (ResetFromTargetInstall && !Target.CanBeUsedAsResetSource()) {
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("target install not suitable for reset error title", ""));
            }

            // Lock source

            Source.UpdateLockStatus();
            if (Source.Locked) {
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("sync source already locked error title", ""));
            }

            if (!Source.LockSource()) {
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("lock sync source failed error title", ""));
            }

            if (backgroundWorker.CancellationPending) {
                Source.UnlockSource();
                e.Cancel = true;
                return;
            }

            // Create temporary directory to do sync in 

            string temporarySourceDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "Source");

            try {
                Directory.CreateDirectory(temporarySourceDirectory);
            } catch (Exception) {
                Source.UnlockSource();
                throw;
            }

            string temporaryDestinationDirectory = Path.Combine(Path.GetDirectoryName(temporarySourceDirectory), "Destination");

            try {
                Directory.CreateDirectory(temporaryDestinationDirectory);
            } catch (Exception) {
                Source.UnlockSource();
                throw;
            }

            WoWInstall syncSourceInstall = null;

            // See if we have a cache of this sync and check if its hash matches the sync store

            string syncId = Source.FetchSyncId();
            string syncHash = Source.FetchSyncDataHash();

            if (syncId == null) {
                syncId = Guid.NewGuid().ToString();
            }

            string cachedSyncLocation = Path.Combine(Path.GetTempPath(), KNBundle.MainBundle().BundleIdentifier, syncId);

            Boolean didUseCachedSyncStore = false;

            if (Directory.Exists(cachedSyncLocation)) {

                WoWInstall cachedInstall = new WoWInstall(cachedSyncLocation);

                if (cachedInstall.CalculateHashForInstallFiles().Equals(syncHash)) {
                    try {
                        syncSourceInstall = cachedInstall.InstallByDuplicatingInstallToDirectory(temporarySourceDirectory);
                        didUseCachedSyncStore = true;

                    } catch (Exception) {
                        //No worry, we'll just unpack it from source later
                    }
                } else {
                    try {
                        DirectoryInfo o = new DirectoryInfo(cachedSyncLocation);
                        o.TryToDelete();
                    } catch (Exception) { }
                }
            }

            if (didUseCachedSyncStore == false || syncSourceInstall == null) {
                // No cache or it didn't hash correctly. Extract from source.

                try {
                    WoWInstall cachedInstall = Source.UnpackSyncSourceToLocationInFileSystem(cachedSyncLocation);
                    syncSourceInstall = cachedInstall.InstallByDuplicatingInstallToDirectory(temporarySourceDirectory);
                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }
            }

            // Check if sync source is suitable for reset

            if (ResetFromSyncSource && !syncSourceInstall.CanBeUsedAsResetSource()) {
                Source.UnlockSource();
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("sync source not suitable for reset error title", ""));
            }

            // Create fresh sync source if we're resetting from target

            if (ResetFromTargetInstall) {
                try {
                    Directory.Delete(temporarySourceDirectory, true);
                    Directory.CreateDirectory(temporarySourceDirectory);

                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }
            }

            if (backgroundWorker.CancellationPending) {
                Source.UnlockSource();
                e.Cancel = true;
                return;
            }

            // Copy the target install to the sync directory, or create a fresh one if it's being reset

            WoWInstall syncTargetInstall = null;

            if (ResetFromSyncSource) {
                syncTargetInstall = new WoWInstall(temporaryDestinationDirectory);
            } else {

                try {
                    syncTargetInstall = Target.InstallByDuplicatingInstallToDirectory(temporaryDestinationDirectory);
                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }
            }

            // Make sure WoW isn't running

            if (Helpers.WoWIsOpen()) {
                Source.UnlockSource();
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("wow open during sync error title", ""));
            }

            // Perform a dry-run of source-target, and back up the target if it'll be changed

            SyncOperation syncOperation = new SyncOperation(syncTargetInstall, syncSourceInstall);

            Boolean targetWillBeChanged = syncOperation.PerformDryRunOfSourceToTargetSync();

            if (backgroundWorker.CancellationPending) {
                Source.UnlockSource();
                e.Cancel = true;
                return;
            }

            if (targetWillBeChanged) {

                // Backup the target install

                try {
                    BackupController.SharedInstance().AddBackupByBackingUpInstallWithDescription(
                        Target,
                        KNBundleGlobalHelpers.KNLocalizedString("pre-sync backup title", "")
                        );
                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }
            }

            if (backgroundWorker.CancellationPending) {
                Source.UnlockSource();
                e.Cancel = true;
                return;
            }

            // Sync!!!

            syncTargetInstall = new WoWInstall(syncTargetInstall.DirectoryPath);
            syncSourceInstall = new WoWInstall(syncSourceInstall.DirectoryPath);

            syncOperation = new SyncOperation(syncTargetInstall, syncSourceInstall);

            SyncLog log = syncOperation.Sync();

            if (log == null) {
                Source.UnlockSource();
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("Unknown error",""));
            }

            if (backgroundWorker.CancellationPending) {
                Source.UnlockSource();
                e.Cancel = true;
                return;
            }

            // Can't cancel any more!

			// MIGRATION: This used to will/didChangeValueForKey on CanCancel
            backgroundWorker.WorkerSupportsCancellation = false;
			// -- didChange here
           
            // Make sure WoW isn't running

            if (Helpers.WoWIsOpen()) {
                Source.UnlockSource();
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("wow open during sync error title", ""));
            }

            // If source changed, pack it up and store again 

            if ((log.Result & SyncLog.SyncResult.kSourceWasUpdated) == SyncLog.SyncResult.kSourceWasUpdated) {

                string newSyncId = null;

                try {
                    newSyncId = Source.PackAndStoreSyncSourceInFileSystem(syncSourceInstall);
                    if (newSyncId == null) {
                        throw new Exception("Unknown error");
                    }
                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }

                cachedSyncLocation = Path.Combine(
                    Path.GetTempPath(),
                    KNBundle.MainBundle().BundleIdentifier,
                    newSyncId
                    );
            }

            // Cache the sync so we don't have to download it each time
            // This is optional, so we don't care if it fails.

            if (!Directory.Exists(cachedSyncLocation)) {
                try {
                    Directory.CreateDirectory(cachedSyncLocation);
                    syncSourceInstall.InstallByDuplicatingInstallToDirectory(cachedSyncLocation);
                } catch (Exception) {
                    // Don't care
                }
            }

            // Make sure WoW isn't running

            if (Helpers.WoWIsOpen()) {
                Source.UnlockSource();
                throw new Exception(KNBundleGlobalHelpers.KNLocalizedString("wow open during sync error title", ""));
            }

            // If the destination was changed, update the user's install with the sync result 

            if ((log.Result & SyncLog.SyncResult.kDestinationWasUpdated) == SyncLog.SyncResult.kDestinationWasUpdated) {

                try {
                    WoWInstall newTarget = syncTargetInstall.InstallByDuplicatingInstallToDirectory(Target.DirectoryPath);
                    if (newTarget == null) {
                        throw new Exception("Unknown error");
                    }
                } catch (Exception) {
                    Source.UnlockSource();
                    throw;
                }
            }

            // Unlock source 

            Source.UnlockSource();

            // Remove temp directory

            try {
                Directory.Delete(Path.GetDirectoryName(temporarySourceDirectory), true);
            } catch (Exception) {

            }

            // All done.

            e.Result = log;

        }

        private void SyncCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if (e.Cancelled) {
                InformDelegateOfCancellation();
            } else if (e.Error != null) {
                FailWithException(e.Error);
            } else {
                SyncLog log = (SyncLog)e.Result;
                SucceedWithLog(log);
            }

            IsSyncing = false;
        }

        #region Properties

        public SyncContainerDelegate Delegate { get; set; }
        public Boolean ResetFromSyncSource { get; set; }
        public Boolean ResetFromTargetInstall { get; set; }

        private SyncSource Source { get; set; }
        private WoWInstall Target { get; set; }
        private Boolean IsSyncing { get; set; }

        #endregion

    }
}
