using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KNFoundation;

namespace TeleportAddons {
    class SyncLogStep {

        public enum SyncLogStepStatus : int {
            kStepStatusFailed = 0,
            kStepStatusSucceeded = 1
        }

        public SyncLogStep() {
            Status = SyncLogStepStatus.kStepStatusSucceeded;
            Children = new List<SyncLogStep>();
        }

        public SyncLogStep(Dictionary<string, object> plistRepresentation) {

            Children = new List<SyncLogStep>();

            if (plistRepresentation != null) {
                StepDescription = (string)plistRepresentation[Constants.kLogStepPlistDescriptionKey];
                Status = (SyncLogStepStatus)plistRepresentation[Constants.kLogStepPlistStatusKey];

                ArrayList childReps = (ArrayList)plistRepresentation[Constants.kLogStepPlistChildrenKey];

                if (childReps != null && (childReps.Count > 0)) {

                    foreach (Dictionary<string, object> rep in childReps) {
                        Children.Add(new SyncLogStep(rep));
                    }
                }
            }
        }

        public Dictionary<string, object> PlistRepresentation() {

            Dictionary<string, object> plist = new Dictionary<string, object>();

			plist[Constants.kLogStepPlistStatusKey] = (int)Status;
			plist[Constants.kLogStepPlistDescriptionKey] = StepDescription;

            if (Children != null && (Children.Count > 0)) {

				List<Dictionary<string, object>> rep = new List<Dictionary<string, object>>();
				foreach (SyncLogStep step in Children)
					rep.Add(step.PlistRepresentation());

				plist[Constants.kLogStepPlistChildrenKey] = rep;
            }
            return plist;
        }

        #region Properties

        public List<SyncLogStep> Children {
            get;
            set;
        }

        public string StepDescription {
            get;
            set;
        }

        public SyncLogStepStatus Status {
            get;
            set;
        }

        #endregion


    }
}
