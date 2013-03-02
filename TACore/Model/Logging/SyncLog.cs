using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using KNFoundation;

namespace TACore {

    public class SyncLog {

        public enum SyncResult : int {
            kNothingChanged = 0,
            kSourceWasUpdated = 1,
            kDestinationWasUpdated = 2
        }

        public SyncLog() {
            Steps = new List<SyncLogStep>();
        }

        public SyncLog(Dictionary<string, object> plistRepresentation) {

            Steps = new List<SyncLogStep>();

            if (plistRepresentation != null) {

				if (plistRepresentation.ContainsKey(Constants.kLogPlistStartDateKey))
                	StartDate = (DateTime)plistRepresentation[Constants.kLogPlistStartDateKey];

				if (plistRepresentation.ContainsKey(Constants.kLogPlistEndDateKey))
					EndDate = (DateTime)plistRepresentation[Constants.kLogPlistEndDateKey];

				if (plistRepresentation.ContainsKey(Constants.kLogPlistWasTestRunKey))
               		WasTestRun = (Boolean)plistRepresentation[Constants.kLogPlistWasTestRunKey];

				if (plistRepresentation.ContainsKey(Constants.kLogPlistResultKey))
                	Result = (SyncResult)plistRepresentation[Constants.kLogPlistResultKey];

				if (!plistRepresentation.ContainsKey(Constants.kLogPlistStepsKey))
					return;

                ArrayList stepRepresentations = (ArrayList)plistRepresentation[Constants.kLogPlistStepsKey];
                
                if (stepRepresentations != null && (stepRepresentations.Count > 0)) {
                    foreach (Dictionary<string, object> stepRep in stepRepresentations)
                        Steps.Add(new SyncLogStep(stepRep));
                }
            }
        }

        public Dictionary<string, object> PlistRepresentation() {

            Dictionary<string, object> plist = new Dictionary<string, object>();

			plist[Constants.kLogPlistStartDateKey] = StartDate;
			plist[Constants.kLogPlistEndDateKey] = EndDate;
			plist[Constants.kLogPlistWasTestRunKey] = WasTestRun;
			plist[Constants.kLogPlistResultKey] = (int)Result;

            if (Steps != null && (Steps.Count > 0)) {

				List<Dictionary<string, object>> stepsRep = new List<Dictionary<string, object>>();
				foreach (SyncLogStep step in Steps)
					stepsRep.Add(step.PlistRepresentation());

				plist[Constants.kLogPlistStepsKey] = stepsRep;
            }

            return plist;
        }

        public string DisplayName() {
            return String.Format(KNBundleGlobalHelpers.KNLocalizedString("log display name formatter", ""),
                StartDate, StartDate);
        }

        public string StatusDescription() {

            if (Result == SyncResult.kNothingChanged) {
                return KNBundleGlobalHelpers.KNLocalizedString("no changes made in sync title", "");

            } else if ((Result & SyncResult.kDestinationWasUpdated) == SyncResult.kDestinationWasUpdated &&
                (Result & SyncResult.kSourceWasUpdated) == SyncResult.kSourceWasUpdated) {

                    return KNBundleGlobalHelpers.KNLocalizedString("changes made to source and destination in sync title", "");

            } else if ((Result & SyncResult.kDestinationWasUpdated) == SyncResult.kDestinationWasUpdated) {

                return KNBundleGlobalHelpers.KNLocalizedString("changes made to destination in sync title", "");
            } else {
                return KNBundleGlobalHelpers.KNLocalizedString("changes made to source in sync title", "");
            }
        }

        #region Properties

        public DateTime StartDate {
            get;
            set;
        }

        public DateTime EndDate {
            get;
            set;
        }

        public List<SyncLogStep> Steps {
            get;
            set;
        }

        public SyncResult Result {
            get;
            set;
        }

        public Boolean WasTestRun {
            get;
            set;
        }

        public string FilePath {
            get;
            set;
        }

        #endregion
    }
}
