using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTools
{
    class Program
    {
        static void Main(string[] args)
        {
            var prjFilePathOrg = @"C:\Users\nadavsof\Dropbox\TheRationVictoryProjects\NAudio-master\NAudioWpfDemo\NAudioWpfDemo.csproj_";
            var prjFileNew  = @"C:\Users\nadavsof\Dropbox\TheRationVictoryProjects\NAudio-master\NAudioWpfDemo\NAudioWpfDemo.csproj_new";

            // <Resource Include="Samples\Letters\300_shin\4heads1.png" />
            // <Content Include="Samples\Letters\2_bet\simple.xml">
            //      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
            // </Content>

            using (var w = new StreamWriter(prjFileNew))
            using (var r = new StreamReader(prjFilePathOrg))
            {
                while (!r.EndOfStream)
                {
                    var line = r.ReadLine();

                    if (line.Contains("<Resource Include"))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine(line.Replace("<Resource Include=", "<Content Include=").Replace("/>", ">"));
                        sb.AppendLine("     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
                        sb.AppendLine("</Content>");
                        w.WriteLine(sb.ToString());
                    }
                    else
                        w.WriteLine(line);
                }
            }

            Console.WriteLine("finished");
            Console.ReadLine();
        }
    }
}
