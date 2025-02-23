
using Menedzser_HSZF_2024251.Model;
using System.Xml.Serialization;

namespace Menedzser_HSZF_2024251.Application
{
    public class XmlReportGenerator : IXmlReportGenerator
    {
        private readonly string _reportPath;
        private readonly XmlSerializer _serializer;

        public XmlReportGenerator(string path)
        {
            _reportPath = path;
            _serializer = new XmlSerializer(typeof(TaskReport));
        }

        public void GenerateReport(TaskReport report, DateTime currentDate)
        {
            string[] fileNames = Directory.GetFiles(_reportPath, $"Report_{currentDate:yyyyMMdd}*.xml");
            string fileName = $"Report_{currentDate:yyyyMMdd}_{fileNames.Length}.xml";
            using (StreamWriter writer = new StreamWriter(Path.Combine(_reportPath, fileName)))
            {
                _serializer.Serialize(writer, report);
            }
        }

        public TaskReport LoadReport(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                return (TaskReport)_serializer.Deserialize(reader);
            }
        }
    }
}
