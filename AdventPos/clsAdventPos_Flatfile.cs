using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdventPos
{
    public class clsAdventPos_Flatfile
    {
        string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");

        public clsAdventPos_Flatfile(int StoreId, decimal tax, List<UPC> upcs)
        {
            try
            {
                AdventConvertRawFile(StoreId, tax, upcs);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static DataTable ConvertCsvToDataTable(string FileName, int StoreId)
        {
            DataTable dtResult = new DataTable();
            try
            {
                using (TextFieldParser parser = new TextFieldParser(FileName))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    int i = 0;
                    int r = 0;
                    while (!parser.EndOfData)
                    {
                        if (i == 0)
                        {
                            string[] columns = parser.ReadFields();
                            foreach (string col in columns)
                            {
                                dtResult.Columns.Add(col);
                            }
                        }
                        else
                        {
                            string[] rows = parser.ReadFields();
                            if (StoreId == 11196)
                            {
                                if (rows.Count() > 8)
                                {
                                    continue;
                                }
                            }
                            dtResult.Rows.Add();
                            int c = 0;
                            foreach (string row in rows)
                            {
                                var roww = row.Replace('"', ' ').Trim();

                                dtResult.Rows[r][c] = roww.ToString();
                                c++;
                            }

                            r++;
                        }
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return dtResult; //Returning Dattable  
        }
        #region AdventConvertRawFile
        public string AdventConvertRawFile(int StoreId, decimal Tax, List<UPC> upcs)
        {

            if (Directory.Exists(BaseUrl))
            {
                if (Directory.Exists(BaseUrl + "/" + StoreId + "/Raw/"))
                {
                    var directory = new DirectoryInfo(BaseUrl + "/" + StoreId + "/Raw/");
                    if (directory.GetFiles().FirstOrDefault() != null)
                    {
                        var myFile = (from f in directory.GetFiles()
                                      orderby f.LastWriteTime descending
                                      select f).First();

                        string Url = BaseUrl + "/" + StoreId + "/Raw/" + myFile;
                        if (File.Exists(Url))
                        {
                            try
                            {
                                DataTable dt = ConvertCsvToDataTable(Url, StoreId);

                                var dtr = from s in dt.AsEnumerable() select s;
                                List<adventProductsModel> prodlist = new List<adventProductsModel>();
                                List<FullnameModel> full = new List<FullnameModel>();

                                foreach (DataRow dr in dt.Rows)
                                {
                                    adventProductsModel pmsk = new adventProductsModel();
                                    FullnameModel fname = new FullnameModel();
                                    if (StoreId == 11196)
                                    {
                                        pmsk.StoreID = StoreId;
                                        string mainupc = Regex.Replace(dr["MAINUPC"].ToString(), @"[^0-9]+", "");
                                        if (!string.IsNullOrEmpty(mainupc))
                                        {
                                            pmsk.sku = "#" + mainupc.ToString();
                                            fname.sku = "#" + mainupc.ToString();
                                        }
                                        string upc = Regex.Replace(dr["MAINUPC"].ToString(), @"[^0-9]+", "");
                                        if (!string.IsNullOrEmpty(upc))
                                        {
                                            pmsk.upc = "#" + upc.ToString();
                                            fname.upc = "#" + upc.ToString();
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                        //pmsk.Qty = Convert.ToInt32(dr["TOTALQTY_MULTI"]);
                                        decimal qty = Convert.ToDecimal(dr["TOTALQTY_MULTI"]);
                                        pmsk.Qty = Convert.ToInt32(qty) > 0 ? Convert.ToInt32(qty) : 0;
                                        if (!string.IsNullOrEmpty(dr.Field<string>("ITEMNAME")))
                                        {
                                            pmsk.StoreProductName = dr.Field<string>("ITEMNAME").Replace("=", "");
                                            fname.pname = dr.Field<string>("ITEMNAME").Replace("=", "");
                                            pmsk.StoreDescription = dr.Field<string>("ITEMNAME").Trim().Replace("=", "");
                                            fname.pdesc = dr.Field<string>("ITEMNAME").Replace("=", "");
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                        string prc = dr["ITEMPRICE"].ToString();
                                        if (!string.IsNullOrEmpty(prc))
                                        {
                                            pmsk.Price = System.Convert.ToDecimal(dr["ITEMPRICE"]);
                                            fname.Price = System.Convert.ToDecimal(dr["ITEMPRICE"]);
                                            if (pmsk.Price <= 0 || fname.Price <= 0)
                                            {
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        pmsk.sprice = 0;
                                        string pack = dr["PACKNAME"].ToString();
                                        if (pack == "Single" || pack == "")
                                        {
                                            pmsk.pack = "1";
                                            fname.pack = "1";
                                        }
                                        else
                                        {
                                            pack = Regex.Replace(pack, @"[^0-9]+", "");
                                            pmsk.pack = pack;
                                            fname.pack = "1";
                                        }
                                        if (pmsk.sprice > 0)
                                        {
                                            pmsk.Start = DateTime.Now.ToString("MM/dd/yyyy");
                                            pmsk.End = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                        }
                                        else
                                        {
                                            pmsk.Start = "";
                                            pmsk.End = "";
                                        }
                                        pmsk.Tax = Tax;
                                        fname.pcat1 = "";
                                        fname.pcat2 = "";
                                        pmsk.uom = dr.Field<string>("SIZENAME");
                                        fname.uom = dr.Field<string>("SIZENAME");
                                        fname.region = "";
                                        fname.country = "";

                                        prodlist.Add(pmsk);
                                        full.Add(fname);
                                    }
                                    else
                                    {
                                        pmsk.StoreID = StoreId;
                                        string sku = Regex.Replace(dr["SKU"].ToString(), @"[^0-9]+", "");
                                        if (!string.IsNullOrEmpty(sku))
                                        {
                                            pmsk.sku = "#" + sku.ToString();
                                            fname.sku = "#" + sku.ToString();
                                        }
                                        string upc = Regex.Replace(dr["UPC"].ToString(), @"[^0-9]+", "");
                                        if (!string.IsNullOrEmpty(upc))
                                        {
                                            pmsk.upc = "#" + upc.ToString();
                                            fname.upc = "#" + upc.ToString();
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        var y = dr["TOTALQTY"].ToString();

                                        if (y != "")
                                        {
                                            var Qtyy = Convert.ToDecimal(y);

                                            //pmsk.Qty = Convert.ToInt32(Qtyy);
                                            pmsk.Qty = Convert.ToInt32(Qtyy) > 0 ? Convert.ToInt32(Qtyy) : 0;
                                        }
                                        else
                                        {
                                            pmsk.Qty = Convert.ToInt32(null);
                                        }
                                        pmsk.StoreProductName = dr.Field<string>("ITEMNAME").Replace("=", "");
                                        fname.pname = dr.Field<string>("ITEMNAME").Replace("=", "");
                                        pmsk.StoreDescription = dr.Field<string>("ITEMNAME").Trim().Replace("=", "");
                                        fname.pdesc = dr.Field<string>("ITEMNAME").Replace("=", "");
                                        pmsk.Price = System.Convert.ToDecimal(dr["Price"]);
                                        fname.Price = System.Convert.ToDecimal(dr["Price"]);
                                        if (pmsk.Price <= 0 || fname.Price <= 0)
                                        {
                                            continue;
                                        }

                                        pmsk.sprice = 0;
                                        string pack = dr["PackName"].ToString();
                                        if (pack == "Single" || pack == "")
                                        {
                                            pmsk.pack = "1";
                                            fname.pack = "1";
                                        }
                                        else
                                        {
                                            pack = Regex.Replace(pack, @"[^0-9]+", "");
                                            pmsk.pack = pack;
                                            fname.pack = "1";
                                        }
                                        if (pmsk.sprice > 0)
                                        {
                                            pmsk.Start = DateTime.Now.ToString("MM/dd/yyyy");
                                            pmsk.End = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                        }
                                        else
                                        {
                                            pmsk.Start = "";
                                            pmsk.End = "";
                                        }
                                        pmsk.Tax = Tax;
                                        fname.pcat1 = "";
                                        fname.pcat2 = "";
                                        pmsk.uom = dr.Field<string>("SizeName");
                                        fname.uom = dr.Field<string>("SizeName");
                                        fname.region = "";
                                        fname.country = "";
                                        prodlist.Add(pmsk);
                                        full.Add(fname);
                                    }
                                }
                                if (upcs != null)
                                {
                                    foreach (var item in upcs)
                                    {
                                        var queryProduct = (from p in prodlist
                                                            where p.upc != item.upccode
                                                            select p).ToList();
                                        prodlist = queryProduct;

                                        var queryFullName = (from f in full
                                                             where f.upc != item.upccode
                                                             select f).ToList();
                                        full = queryFullName;
                                    }
                                }

                                Console.WriteLine("Generating AdventPos " + StoreId + " Product CSV Files.....");
                                string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", StoreId, BaseUrl);
                                Console.WriteLine("Product File Generated For AdventPos " + StoreId);
                                Console.WriteLine();
                                Console.WriteLine("Generating AdventPos " + StoreId + " Fullname CSV Files.....");
                                filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", StoreId, BaseUrl);
                                Console.WriteLine("Fullname File Generated For AdventPos " + StoreId);

                                string[] filePaths = Directory.GetFiles(BaseUrl + "/" + StoreId + "/Raw/");

                                foreach (string filePath in filePaths)
                                {
                                    string destpath = filePath.Replace(@"/Raw/", @"/RawDeleted/" + DateTime.Now.ToString("yyyyMMddhhmmss"));
                                    File.Move(filePath, destpath);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("" + e.Message);
                                return "Not generated file for AdventPos " + StoreId;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ínvalid FileName or RAW folder is empty!" + StoreId);
                        return "";
                    }
                }
                else
                {
                    return "Invalid Sub-Directory " + StoreId;
                }
            }
            else
            {
                return "Invalid Directory " + StoreId;
            }
            return "Completed generating File";

        }
        #endregion
    }
    public class adventProductsModel  // For Mapping Tool of AdventPos
    {
        public int StoreID { get; set; }
        public string upc { get; set; }
        public int Qty { get; set; }
        public string sku { get; set; }
        public string pack { get; set; }
        public string uom { get; set; }
        public string StoreProductName { get; set; }
        public string StoreDescription { get; set; }
        public decimal Price { get; set; }
        public decimal sprice { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public decimal Tax { get; set; }
        public string altupc1 { get; set; }
        public string altupc2 { get; set; }
        public string altupc3 { get; set; }
        public string altupc4 { get; set; }
        public string altupc5 { get; set; }

    }
}
