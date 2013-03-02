using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;

namespace TACore {
    class LogController {

        private static LogController sharedInstance;

        public static LogController SharedInstance() {
            if (sharedInstance == null) {
                sharedInstance = new LogController();
            }
            return sharedInstance;
        }
        
        private LogController() {

            List<SyncLog> logs = new List<SyncLog>();
            DirectoryInfo logPathInfo = new DirectoryInfo(LogsPath());

            foreach (FileInfo file in logPathInfo.GetFiles()) {

                if (file.Extension.IndexOf(Constants.kLogFileExtension, StringComparison.CurrentCultureIgnoreCase) != -1) {

                    try {

                        Dictionary<string, object> plist = KNPropertyListSerialization.PropertyListWithData(File.ReadAllBytes(file.FullName));
                        if (plist != null) {
                            SyncLog log = new SyncLog(plist);
                            log.FilePath = file.FullName;
                            logs.Add(log);
                        }
                    } catch (Exception) { }

                }
            }

            logs.Sort(delegate(SyncLog log1, SyncLog log2) {
                return log1.StartDate.CompareTo(log2.StartDate) * -1;
            });

            Logs = logs;
        }

        public void AddLog(SyncLog log) {

            if (log == null) {
                return;
            }

            if (Logs.Count >= Constants.kLogCount) {
                Logs.Sort(delegate(SyncLog log1, SyncLog log2) {
                    return log1.StartDate.CompareTo(log2.StartDate) * -1;
                });
                RemoveLog(Logs[Logs.Count - 1]);
            }

            string logPath = PathForNewLogAtDate(log.StartDate);
            byte[] logData = KNPropertyListSerialization.DataWithPropertyList(log.PlistRepresentation());

            try {
                File.WriteAllBytes(logPath, logData);
                log.FilePath = logPath;
                Logs.Insert(0, log);
            } catch (Exception) {
             
            }

        }

        public void RemoveLog(SyncLog log) {

            if (Logs.Contains(log)) {
                Logs.Remove(log);
                try {
                    File.Delete(log.FilePath);
                } catch (Exception) {
                }
            }

        } 

        private string LogsPath() {

            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logsFolderPath = Path.Combine(appDataFolderPath, Constants.kApplicationSupportFolderName, Constants.kLogsFolderName);

            if (!Directory.Exists(logsFolderPath)) {
                try {
                    Directory.CreateDirectory(logsFolderPath);
                } catch (Exception) {
                    logsFolderPath = Path.GetTempPath();
                }
            }

            return logsFolderPath;
        }

        private string PathForNewLogAtDate(DateTime aDate) {

            string logFolderPath = LogsPath();
            string logFileName = String.Format(KNBundleGlobalHelpers.KNLocalizedString("log file name formatter", ""),
                aDate, aDate);

            logFileName = logFileName.Replace("/", "-");
            logFileName = logFileName.Replace(":", "-");

            string logFilePath = Helpers.PathByAppendingUniqueFileNameFromFileNameInPath(logFileName, logFolderPath);
            return logFilePath;
        }

        

        #region Properties 

        public List<SyncLog> Logs {
            get;
            private set;
        }

        #endregion


    }
}
