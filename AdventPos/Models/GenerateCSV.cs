using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AdventPos
{
    public class GenerateCSV
    {
        public static string GenerateCSVFile<T>(IList<T> list, string Name, int StoreId, string BaseUrl, IEnumerable<string> excludeColumns = null)//Changed by PK on 30-01-2026
        {
            if (list == null || list.Count == 0) return "";
            if (!Directory.Exists(BaseUrl + "\\" + StoreId + "\\Upload\\"))
            {
                Directory.CreateDirectory(BaseUrl + "\\" + StoreId + "\\Upload\\");
            }
            string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
            string fcname = BaseUrl + "\\" + StoreId + "\\Upload\\" + filename;
            var excluded = excludeColumns != null
                ? new HashSet<string>(excludeColumns, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();//Added by PK on 30-01-2026
            Type t = list[0].GetType();
            string newLine = Environment.NewLine;

            using (var sw = new StreamWriter(fcname))
            {
                object o = Activator.CreateInstance(t);
                PropertyInfo[] props = o.GetType().GetProperties().Where(p => !excluded.Contains(p.Name)).ToArray();//Added by PK on 30-01-2026
                foreach (PropertyInfo pi in props)
                {
                    sw.Write(pi.Name + ",");
                }
                sw.Write(newLine);
                foreach (T item in list)
                {
                    foreach (PropertyInfo pi in props)
                    {
                        string whatToWrite =
                            Convert.ToString(item.GetType()
                                                 .GetProperty(pi.Name)
                                                 .GetValue(item, null))
                                .Replace(',', ' ') + ',';

                        sw.Write(whatToWrite);

                    }
                    sw.Write(newLine);
                }
                return filename;
            }
        }
    }

}



