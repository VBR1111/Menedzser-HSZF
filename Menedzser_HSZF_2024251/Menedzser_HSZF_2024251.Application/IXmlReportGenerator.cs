using Menedzser_HSZF_2024251.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menedzser_HSZF_2024251.Application
{
    public interface IXmlReportGenerator
    {
        void GenerateReport(TaskReport report, DateTime currentDate);
        TaskReport LoadReport(string fileName);

    }
}
