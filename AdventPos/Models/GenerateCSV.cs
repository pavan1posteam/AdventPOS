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
        public static string GenerateCSVFile(DataTable dt, string Name, int StoreId)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                int count = 1;
                int totalColumns = dt.Columns.Count;
                foreach (DataColumn dr in dt.Columns)
                {
                    sb.Append(dr.ColumnName);

                    if (count != totalColumns)
                    {
                        sb.Append(",");
                    }

                    count++;
                }

                sb.AppendLine();

                string value = String.Empty;
                foreach (DataRow dr in dt.Rows)
                {
                    for (int x = 0; x < totalColumns; x++)
                    {
                        value = dr[x].ToString();

                        if (value.Contains(",") || value.Contains("\""))
                        {
                            value = '"' + value.Replace("\"", "\"\"") + '"';
                        }

                        sb.Append(value);

                        if (x != (totalColumns - 1))
                        {
                            sb.Append(",");
                        }
                    }

                    sb.AppendLine();
                }
                if (!Directory.Exists("Upload\\"))
                {
                    Directory.CreateDirectory("Upload\\");
                }
                string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
                File.WriteAllText("Upload\\" + filename, sb.ToString());
                return filename;
            }
            catch (Exception)
            {
                // Do something
            }
            return "";
        }

        public static string GenerateCSVFile<T>(IList<T> list, string Name, int StoreId, string BaseUrl)
        {

            if (list == null || list.Count == 0) return "";
            if (!Directory.Exists(BaseUrl + "\\" + StoreId + "\\Upload\\"))
            {
                Directory.CreateDirectory(BaseUrl + "\\" + StoreId + "\\Upload\\");
            }
            string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
            string fcname = BaseUrl + "\\" + StoreId + "\\Upload\\" + filename;
            // Console.WriteLine("Generating " + filename + " ........");
            //File.WriteAllText(BaseUrl + "\\" + StoreId + "\\Upload\\" + filename, csvData.ToString());
            // return filename;

            //get type from 0th member
            Type t = list[0].GetType();
            string newLine = Environment.NewLine;

            using (var sw = new StreamWriter(fcname))
            {
                //make a new instance of the class name we figured out to get its props
                object o = Activator.CreateInstance(t);
                //gets all properties
                PropertyInfo[] props = o.GetType().GetProperties();

                //foreach of the properties in class above, write out properties
                //this is the header row
                foreach (PropertyInfo pi in props)
                {
                    sw.Write(pi.Name + ",");
                }
                sw.Write(newLine);

                //this acts as datarow
                foreach (T item in list)
                {
                    //this acts as datacolumn
                    foreach (PropertyInfo pi in props)
                    {
                        //this is the row+col intersection (the value)
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

        //public static string GenerateCSVFile(List<ProductModel> list, string Name, int StoreId, string BaseUrl)
        //{
        //    StringBuilder csvData = null;
        //    try
        //    {
        //        csvData = new StringBuilder();
        //        //Get the properties for type T for the headers
        //        PropertyInfo[] propInfos = typeof(ProductModel).GetProperties();
        //        for (int i = 0; i <= propInfos.Length - 1; i++)
        //        {
        //            csvData.Append(propInfos[i].Name);
        //            if (i < propInfos.Length - 1)
        //            {
        //                csvData.Append(",");
        //            }
        //        }
        //        csvData.AppendLine();

        //        //Loop through the collection, then the properties and add the values
        //        for (int i = 0; i <= list.Count - 1; i++)
        //        {
        //            ProductModel item = list[i];
        //            for (int j = 0; j <= propInfos.Length - 1; j++)
        //            {
        //                object csvProperty = item.GetType().GetProperty(propInfos[j].Name).GetValue(item, null);
        //                if (csvProperty != null)
        //                {
        //                    string value = csvProperty.ToString();
        //                    //Check if the value contans a comma and place it in quotes if so
        //                    //if (value.Contains(","))
        //                    //{
        //                    //    value = string.Concat("\"", value, "\"");
        //                    //}
        //                    ////Replace any \r or \n special characters from a new line with a space
        //                    //if (value.Contains("\r"))
        //                    //{
        //                    //    value = value.Replace("\r", " ");
        //                    //}
        //                    //if (value.Contains("\n"))
        //                    //{
        //                    //    value = value.Replace("\n", " ");
        //                    //}

        //                    value = '"' + value.Replace("\"", "\"\"") + '"';

        //                    csvData.Append(value);
        //                }
        //                if (j < propInfos.Length - 1)
        //                {
        //                    csvData.Append(",");
        //                }
        //            }
        //            csvData.AppendLine();
        //        }

        //        if (!Directory.Exists(BaseUrl + "\\" + StoreId + "\\Upload\\"))
        //        {
        //            Directory.CreateDirectory(BaseUrl + "\\" + StoreId + "\\Upload\\");
        //        }
        //        string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
        //        File.WriteAllText(BaseUrl + "\\" + StoreId + "\\Upload\\" + filename, csvData.ToString());
        //        return filename;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //        // Do something
        //    }
        //}

        //public static string GenerateCSVFile(List<ProductModelEcrs> list, string Name, int StoreId, string BaseUrl)
        //{
        //    StringBuilder csvData = null;
        //    try
        //    {
        //        csvData = new StringBuilder();
        //        //Get the properties for type T for the headers
        //        PropertyInfo[] propInfos = typeof(ProductModelEcrs).GetProperties();
        //        for (int i = 0; i <= propInfos.Length - 1; i++)
        //        {
        //            csvData.Append(propInfos[i].Name);
        //            if (i < propInfos.Length - 1)
        //            {
        //                csvData.Append(",");
        //            }
        //        }
        //        csvData.AppendLine();

        //        //Loop through the collection, then the properties and add the values
        //        for (int i = 0; i <= list.Count - 1; i++)
        //        {
        //            ProductModelEcrs item = list[i];
        //            for (int j = 0; j <= propInfos.Length - 1; j++)
        //            {
        //                object csvProperty = item.GetType().GetProperty(propInfos[j].Name).GetValue(item, null);
        //                if (csvProperty != null)
        //                {
        //                    string value = csvProperty.ToString();
        //                    //Check if the value contans a comma and place it in quotes if so
        //                    if (value.Contains(","))
        //                    {
        //                        value = string.Concat("\"", value, "\"");
        //                    }
        //                    //Replace any \r or \n special characters from a new line with a space
        //                    if (value.Contains("\r"))
        //                    {
        //                        value = value.Replace("\r", " ");
        //                    }
        //                    if (value.Contains("\n"))
        //                    {
        //                        value = value.Replace("\n", " ");
        //                    }
        //                    csvData.Append(value);
        //                }
        //                if (j < propInfos.Length - 1)
        //                {
        //                    csvData.Append(",");
        //                }
        //            }
        //            csvData.AppendLine();
        //        }

        //        if (!Directory.Exists(BaseUrl + "\\" + StoreId + "\\Upload\\")) { Directory.CreateDirectory(BaseUrl + "\\" + StoreId + "\\Upload\\"); }
        //        string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
        //        File.WriteAllText(BaseUrl + "\\" + StoreId + "\\Upload\\" + filename, csvData.ToString());
        //        return filename;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //        // Do something
        //    }

        //}
        //public static string GenerateCSVFile(List<ProductModelShopKeep> list, string Name, int StoreId, string BaseUrl)
        //{
        //    StringBuilder csvData = null;
        //    try
        //    {
        //        csvData = new StringBuilder();
        //        //Get the properties for type T for the headers
        //        PropertyInfo[] propInfos = typeof(ProductModelEcrs).GetProperties();
        //        for (int i = 0; i <= propInfos.Length - 1; i++)
        //        {
        //            csvData.Append(propInfos[i].Name);
        //            if (i < propInfos.Length - 1)
        //            {
        //                csvData.Append(",");
        //            }
        //        }
        //        csvData.AppendLine();

        //        //Loop through the collection, then the properties and add the values
        //        for (int i = 0; i <= list.Count - 1; i++)
        //        {
        //            ProductModelShopKeep item = list[i];
        //            for (int j = 0; j <= propInfos.Length - 1; j++)
        //            {
        //                object csvProperty = item.GetType().GetProperty(propInfos[j].Name).GetValue(item, null);
        //                if (csvProperty != null)
        //                {
        //                    string value = csvProperty.ToString();
        //                    //Check if the value contans a comma and place it in quotes if so
        //                    if (value.Contains(","))
        //                    {
        //                        value = string.Concat("\"", value, "\"");
        //                    }
        //                    //Replace any \r or \n special characters from a new line with a space
        //                    if (value.Contains("\r"))
        //                    {
        //                        value = value.Replace("\r", " ");
        //                    }
        //                    if (value.Contains("\n"))
        //                    {
        //                        value = value.Replace("\n", " ");
        //                    }
        //                    csvData.Append(value);
        //                }
        //                if (j < propInfos.Length - 1)
        //                {
        //                    csvData.Append(",");
        //                }
        //            }
        //            csvData.AppendLine();
        //        }

        //        if (!Directory.Exists(BaseUrl + "\\" + StoreId + "\\Upload\\")) { Directory.CreateDirectory(BaseUrl + "\\" + StoreId + "\\Upload\\"); }
        //        string filename = Name + StoreId + DateTime.Now.ToString("yyyyMMddhhmmss") + ".csv";
        //        File.WriteAllText(BaseUrl + "\\" + StoreId + "\\Upload\\" + filename, csvData.ToString());
        //        return filename;
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //        // Do something
        //    }

        //}
        public static void ToCSV(DataTable dtDataTable, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);
            //headers  
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                sw.Write(dtDataTable.Columns[i]);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);
            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }



    }

}



