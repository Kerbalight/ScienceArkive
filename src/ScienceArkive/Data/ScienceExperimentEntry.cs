using KSP.Game.Science;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScienceArkive.Data
{
    public class ScienceExperimentEntry
    {
        public string ExperimentID { get { return Definition.ExperimentID; } }
        public ExperimentDefinition Definition { get; set; }
        public ScienceSitutation Situation { get; set; }
        public string BodyName { get; set; }
        public string RegionName { get; set; }
        public CompletedResearchReport? Report { get; set; }

        public ScienceExperimentEntry(ExperimentDefinition definition, ScienceSitutation situation, string bodyName, string regionName, CompletedResearchReport? report)
        {
            Definition = definition;
            Situation = situation;
            BodyName = bodyName;
            RegionName = regionName;
            Report = report;
        }

    }
}
