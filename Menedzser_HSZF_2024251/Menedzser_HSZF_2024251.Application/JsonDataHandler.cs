using Menedzser_HSZF_2024251.Model;
using Newtonsoft.Json;
using System.Xml;

namespace Menedzser_HSZF_2024251.Application
{
    public class JsonDataHandler
    {
        public static FootballData LoadData(string filePath)
        {
            string jsonContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<FootballData>(jsonContent,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
        }
    }
}
