using System.Xml.Serialization;

namespace Menedzser_HSZF_2024251.Model
{
    [XmlRoot("TaskReport")]
    public class TaskReport
    {
        [XmlElement("TaskName")]
        public string TaskName { get; set; }

        [XmlElement("TeamName")]
        public string TeamName { get; set; }

        [XmlElement("ExecutionDate")]
        public DateTime ExecutionDate { get; set; }

        [XmlElement("Success")]
        public bool Success { get; set; }

        [XmlArray("AffectedPlayers")]
        [XmlArrayItem("Player")]
        public List<string> AffectedPlayers { get; set; }

        [XmlElement("RemainingBudget")]
        public decimal RemainingBudget { get; set; }
    }
}
