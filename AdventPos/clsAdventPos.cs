
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
//using System.Xaml;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AdventPos
{
    public class clsAdventPos
    {
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        public List<Datum> products(int StoreId, decimal tax, string BaseUrl, string Username, string Password, string Pin)
        {

            List<JArray> productList = new List<JArray>();
            List<Datum> productList1 = new List<Datum>();

            try
            {
                if (string.IsNullOrEmpty(BaseUrl))
                    BaseUrl = "https://dataservices.sypramsoftware.com/api/Item/GetItemList";
                else if (Regex.IsMatch(BaseUrl, @"com$"))
                    BaseUrl += "/api/Item/GetItemList";
                string authInfo = Username + ":" + Password + ":" + Pin;
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                string content = null;
                clsProductList obj = new clsProductList();
                BaseUrl = string.IsNullOrEmpty(obj.Url) ? BaseUrl : obj.Url;
                var client = new RestClient(BaseUrl);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Basic " + authInfo);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Accept", "application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);              
                content = response.Content;
                //File.AppendAllText("12167.json",response.Content);
               
                if (content == "Unauthorized" || content == "" || content == "null")
                {                
                    (new Email()).sendEmail(DeveloperId, "", "", "Error in " + StoreId + " Advent Pos@" + DateTime.UtcNow + " GMT", " ERROR In Response " + ":" + response.StatusCode);
                }
                else
                {
                    var result = JsonConvert.DeserializeObject<clsProductList.items>(content);
                    var pJson = (dynamic)JObject.Parse(content);
                    var jArray = (JArray)pJson["Data"];
                    productList.Add(jArray);

                    Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(content);
                    productList1 = myDeserializedClass.Data;
                }
            }
            catch (Exception)
            {

            }
            return productList1;
        }
    }

    public class CsvProducts
    {
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        string prcFromPrcA = ConfigurationManager.AppSettings["PriceFromPriceA"];
        string DifferentResponce = ConfigurationManager.AppSettings["DifferentResponce"];
        public CsvProducts(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            productForCSV(storeid, tax, BaseUrl, Username, Password, Pin, IsMarkUpPrice, MarkUpValue);
        }
        public void productForCSV(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            try
            {
                clsAdventPos products = new clsAdventPos();
                var productList = products.products(storeid, tax, BaseUrl, Username, Password, Pin);

                BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
               
                List<datatableModel> pf = new List<datatableModel>();
                if (productList != null)
                {
                    if (productList.Count > 0)
                    {

                        foreach (var item in productList)
                        {
                            datatableModel pdf = new datatableModel();
                            pdf.StoreID = storeid;
                            decimal result;
                            string upc = item.Upc;
                            Decimal.TryParse(upc, System.Globalization.NumberStyles.Float, null, out result);
                            upc = result.ToString();
                            if (string.IsNullOrEmpty(upc))
                                continue;
                            //else if (upc == "0")
                            //    pdf.upc = "";
                            else
                                pdf.upc = upc;
                            pdf.sku = item.Sku;
                            decimal qty = Convert.ToDecimal(item.TotalQty);
                            pdf.Qty = Convert.ToInt32(qty) > 0 ? Convert.ToInt32(qty) : 0;
                            pdf.pack = item.PackName;
                            if (storeid == 11858 && pdf.pack == "18" || pdf.pack == "24" || pdf.pack == "30")
                                continue;
                            pdf.StoreProductName = item.ItemName;
                            pdf.StoreDescription = item.ItemName;
                            pdf.Price = Convert.ToDecimal(item.Price);
                            if (prcFromPrcA.Contains(storeid.ToString()))
                                pdf.Price = Convert.ToDecimal(item.PriceA) == 0 ? Convert.ToDecimal(item.Price) : Convert.ToDecimal(item.PriceA);
                            pdf.sprice = Convert.ToDecimal(item.SalePrice);
                            pdf.Start = "";
                            pdf.End = "";
                            pdf.Tax = tax;
                            pdf.altupc1 = item.AltUpc1;
                            pdf.altupc2 = item.AltUpc2;
                            pdf.altupc3 = "";
                            pdf.altupc4 = "";
                            pdf.altupc5 = "";
                            pdf.uom = item.SizeName;
                            pdf.pcat = item.Department;

                            pf.Add(pdf);
                        }
                        Datatabletocsv csv = new Datatabletocsv();
                        csv.Datatablecsv(storeid, tax, pf,IsMarkUpPrice,MarkUpValue);
                    }
                }
                else
                {
                    new Email().sendEmail(DeveloperId, "", "", "ProductList is  '" + productList.Count + "'" + storeid + "AdventPOS@" + DateTime.UtcNow + " GMT", productList.Count + "<br/>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
    class clsAdventPos_Flatfile
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
                                        if(pmsk.Price <=0 || fname.Price <=0)
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
    public class clsProductList
    {
        public bool StatusVal { get; set; }
        public int StatusCode { get; set; }
        public string StatusMsg { get; set; }
        public string Price { get; set; }
        public string PackName { get; set; } // added 
        public string SessionID { get; set; }

        public string Url { get; set; }
        public class Data
        {
            [JsonProperty("upc")]
            public string UPC { get; set; }
            [JsonProperty("upc")]
            public int SKU { get; set; }
            [JsonProperty("upc")]
            public string ItemName { get; set; }
            [JsonProperty("upc")]
            public double Price { get; set; }
            [JsonProperty("upc")]
            public double Cost { get; set; }
            [JsonProperty("upc")]
            public double SALEPRICE { get; set; }
            [JsonProperty("upc")]
            public string SizeName { get; set; }
            [JsonProperty("upc")]
            public string PackName { get; set; }
            [JsonProperty("upc")]
            public string Vintage { get; set; }
            [JsonProperty("upc")]
            public string Department { get; set; }
            [JsonProperty("upc")]
            public double PriceA { get; set; }
            [JsonProperty("upc")]
            public double PriceB { get; set; }
            [JsonProperty("upc")]
            public double PriceC { get; set; }
            [JsonProperty("upc")]
            public double TotalQty { get; set; }
            [JsonProperty("upc")]
            public string ALTUPC1 { get; set; }
            [JsonProperty("upc")]
            public string ALTUPC2 { get; set; }
            [JsonProperty("upc")]
            public int STORECODE { get; set; }
        }

        public class items
        {
            public List<Data> item { get; set; }
        }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Datum
    {
        [JsonProperty("upc")]
        public string Upc { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("itemname")]
        public string ItemName { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("cost")]
        public double Cost { get; set; }

        [JsonProperty("saleprice")]
        public double SalePrice { get; set; }

        [JsonProperty("sizename")]
        public string SizeName { get; set; }

        [JsonProperty("packname")]
        public string PackName { get; set; }

        [JsonProperty("vintage")]
        public string Vintage { get; set; }

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("pricea")]
        public double PriceA { get; set; }

        [JsonProperty("priceb")]
        public double PriceB { get; set; }

        [JsonProperty("pricec")]
        public double PriceC { get; set; }

        [JsonProperty("totalqty")]
        public double TotalQty { get; set; }

        [JsonProperty("altupc1")]
        public string AltUpc1 { get; set; }

        [JsonProperty("altupc2")]
        public string AltUpc2 { get; set; }

        [JsonProperty("storecode")]
        public string StoreCode { get; set; }
    }

    public class Root
    {
        [JsonProperty("StatusVal")]
        public bool StatusVal { get; set; }

        [JsonProperty("StatusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("StatusMsg")]
        public string StatusMsg { get; set; }

        [JsonProperty("SessionID")]
        public string SessionID { get; set; }

        [JsonProperty("Data")]
        public List<Datum> Data { get; set; }

        [JsonProperty("ExtraData")]
        public object ExtraData { get; set; }
    }

    public class datatableModel
    {
        public int StoreID { get; set; }
        public string upc { get; set; }
        public decimal Qty { get; set; }
        public string sku { get; set; }
        public string pack { get; set; }
        public string uom { get; set; }
        public string pcat { get; set; }
        public string pcat1 { get; set; }
        public string pcat2 { get; set; }
        public string country { get; set; }
        public string region { get; set; }
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
        public double Deposit { get; set; }
    }
    public class ListtoDataTableConverter
    {
        public DataTable ToDataTable<T>(List<T> items, int StoreId)
        {
            DataTable dt = new DataTable(typeof(T).Name);

            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in Props)
            {
                dt.Columns.Add(prop.Name);
            }

            foreach (T item in items)
            {
                var values = new object[Props.Length];

                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dt.Rows.Add(values);
            }
            return dt;
        }
    }
    public class Datatabletocsv
    {
        string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
        string Upcs = ConfigurationManager.AppSettings.Get("Upc");
        string Upc_not_null = ConfigurationManager.AppSettings.Get("Upc_not_null");
        string altupc = ConfigurationManager.AppSettings.Get("altupc");
        string In_Stocks = ConfigurationManager.AppSettings.Get("In_Stocks");
        string BeerDeposit = ConfigurationManager.AppSettings.Get("BeerDeposit");
        string Discount_SalePrice = ConfigurationManager.AppSettings.Get("Discount_SalePrice");
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        string IrrespectiveOfStock = ConfigurationManager.AppSettings["IrrespectiveOfStock"];
        public void Datatablecsv(int storeid, decimal tax, List<datatableModel> dtlist, bool IsMarkUpPrice, int MarkUpValue)
        {
            string DifferentResponce = ConfigurationManager.AppSettings["DifferentResponce"];
            try
            {
                ListtoDataTableConverter cvr = new ListtoDataTableConverter();

                DataTable dt = cvr.ToDataTable(dtlist, storeid);
                var dtr = from s in dt.AsEnumerable() select s;
                List<ProductsModel> prodlist = new List<ProductsModel>();
                List<FullNameProductModel> full = new List<FullNameProductModel>();
                
                dynamic upcs;
                int barlenth = 0;
                #region Store 12167 Discountable
                if (storeid == 12167)
                {
                    List<ProductsModel12167> prodlist12167 = new List<ProductsModel12167>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        ProductsModel12167 pmsk = new ProductsModel12167();
                        FullNameProductModel fname = new FullNameProductModel();
                        dt.DefaultView.Sort = "sku";
                        upcs = dt.DefaultView.FindRows(dr["sku"]).ToArray();
                        barlenth = ((Array)upcs).Length;
                        pmsk.StoreID = storeid;
                        if (barlenth > 0)
                        {
                            for (int i = 0; i <= barlenth - 1; i++)
                            {
                                if (i == 0)
                                {
                                    if (!string.IsNullOrEmpty(dr["upc"].ToString()))
                                    {
                                        var upc = "#" + upcs[i]["upc"].ToString().ToLower();
                                        string numberUpc = Regex.Replace(upc, "[^0-9.]", "");
                                        if (numberUpc == "01")
                                        {
                                        }
                                        if (!Upcs.Contains(storeid.ToString()))
                                        {
                                            if (!string.IsNullOrEmpty(numberUpc))
                                            {
                                                pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                fname.upc = "#" + numberUpc.Trim().ToLower();
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                if (i == 1)
                                {
                                    pmsk.altupc1 = "#" + upcs[i]["upc"];
                                }
                                if (i == 2)
                                {
                                    pmsk.altupc2 = "#" + upcs[i]["upc"];
                                }
                                if (i == 3)
                                {
                                    pmsk.altupc3 = "#" + upcs[i]["upc"];
                                }
                                if (i == 4)
                                {
                                    pmsk.altupc4 = "#" + upcs[i]["upc"];
                                }
                                if (i == 5)
                                {
                                    pmsk.altupc5 = "#" + upcs[i]["upc"];
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(dr["sku"].ToString()))
                        {
                            pmsk.sku = "#" + dr["sku"].ToString();
                            fname.sku = "#" + dr["sku"].ToString();
                        }
                        else
                        { continue; }

                        pmsk.Qty = Convert.ToInt32(dr["Qty"]);

                        var productname = string.IsNullOrEmpty(dr["StoreProductName"].ToString()) ? "" : dr["StoreProductName"].ToString().Trim();
                        if (string.IsNullOrEmpty(productname))
                            productname = "";
                        pmsk.StoreProductName = productname;
                        pmsk.StoreDescription = productname;
                        fname.pdesc = productname;
                        fname.pname = productname;
                        if (IsMarkUpPrice) // Added as per ticket 15515 Store 11445
                        {
                            decimal price = Convert.ToDecimal(dr["Price"]);
                            decimal markup = price * MarkUpValue / 100 + price;
                            pmsk.Price = (markup);
                            pmsk.Price = Decimal.Round(pmsk.Price, 2);
                            fname.Price = Decimal.Round(pmsk.Price, 2);
                        }
                        else if (!string.IsNullOrEmpty(dr["Price"].ToString()))
                        {
                            pmsk.Price = Convert.ToDecimal(dr["Price"]);
                            fname.Price = Convert.ToDecimal(dr["Price"]);
                        }
                        if (pmsk.Price <= 0 || fname.Price <= 0)
                        {
                            continue;
                        }

                        string pak = dr.Field<string>("pack");
                        if (string.IsNullOrEmpty(pak))
                            pak = "1";
                        pmsk.pack = Regex.Replace(pak, @"[^0-9]", "").Trim();// Changed for #45262 (@"[^1-9]")
                        if (string.IsNullOrEmpty(pmsk.pack))
                            pmsk.pack = "1";
                        pmsk.Tax = Convert.ToDecimal(dr["Tax"]);
                        if (pmsk.sprice > 0 && !DifferentResponce.Contains(storeid.ToString()))
                        {
                            pmsk.Start = DateTime.Now.ToString("MM/dd/yyyy");
                            pmsk.End = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            pmsk.Start = "";
                            pmsk.End = "";
                        }
                        fname.pcat = dr.Field<string>("pcat");
                        if (string.IsNullOrEmpty(fname.pcat))
                            fname.pcat = "";
                        if ((fname.pcat.ToLower().Contains("toba") || fname.pcat.ToLower().Contains("lighter") ||
                             fname.pcat.ToLower().Contains("cig") || fname.pcat.ToLower().Contains("lotto")) && storeid != 10708 && storeid != 10350)
                        {
                            continue;
                        }
                        decimal fractionalPart = pmsk.Price - Math.Floor(pmsk.Price);
                        if (fname.pcat.ToLower().Contains("wine"))
                        {
                            if (fractionalPart == 0.00m || fractionalPart == 0.09m || fractionalPart == 0.97m)
                            {
                                pmsk.Discountable = 1;
                            }
                            else
                            {
                                pmsk.Discountable = 0;
                            }
                        }
                        else
                        {
                            pmsk.Discountable = 1;
                        }
                        fname.pcat1 = string.IsNullOrEmpty(dr["pcat1"].ToString()) ? "" : dr["pcat1"].ToString().Trim();
                        fname.pcat2 = "";
                        fname.pack = pmsk.pack;
                        pmsk.uom = string.IsNullOrEmpty(dr["uom"].ToString()) ? "" : dr["uom"].ToString().Trim();
                        fname.uom = pmsk.uom;
                        fname.region = "";
                        fname.country = "";
                        if (pmsk.pack == "1")
                        {
                            pmsk.pack = getpack(pmsk.StoreProductName).ToString();
                            fname.pack = pmsk.pack;
                        }
                        if (string.IsNullOrEmpty(pmsk.uom))
                        {
                            pmsk.uom = getVolume(pmsk.StoreProductName);
                            fname.uom = pmsk.uom;
                        }
                        if (!string.IsNullOrEmpty(pmsk.upc) && fname.pcat != "Cigarette" || fname.pcat != "Lotto" || fname.pcat != "Cigars" || fname.pcat != "TOBACCO" || fname.pcat != "CIG" || fname.pcat != "CIGARILLOS" || fname.pcat != "CIGAR" || fname.pcat != "CIGERATT" || fname.pcat != "CIGERATTE" || fname.pcat != "Tobacco")
                        {
                            prodlist12167.Add(pmsk);
                            prodlist12167 = prodlist12167.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                            prodlist12167 = prodlist12167.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                            full.Add(fname);
                            full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                            full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                        }
                    }
                    Console.WriteLine("Generating ADVENTPOS " + storeid + " Product CSV Files.....");
                    string filename1 = GenerateCSV.GenerateCSVFile(prodlist12167, "PRODUCT", storeid, BaseUrl);
                    Console.WriteLine("Product File Generated For ADVENTPOS " + storeid);
                }
                #endregion
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        ProductsModel pmsk = new ProductsModel();
                        FullNameProductModel fname = new FullNameProductModel();
                        dt.DefaultView.Sort = "sku";
                        upcs = dt.DefaultView.FindRows(dr["sku"]).ToArray();
                        barlenth = ((Array)upcs).Length;
                        pmsk.StoreID = storeid;
                        if (barlenth > 0)
                        {
                            for (int i = 0; i <= barlenth - 1; i++)
                            {
                                if (i == 0)
                                {
                                    if (!string.IsNullOrEmpty(dr["upc"].ToString()))
                                    {
                                        var upc = "#" + upcs[i]["upc"].ToString().ToLower();
                                        string numberUpc = Regex.Replace(upc, "[^0-9.]", "");
                                        if (numberUpc == "01")
                                        {
                                        }
                                        if (Upcs.Contains(storeid.ToString()))
                                        {
                                            if (!string.IsNullOrEmpty(numberUpc))
                                            {
                                                pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                fname.upc = "#" + numberUpc.Trim().ToLower();
                                            }
                                        }
                                        else if (!Upcs.Contains(storeid.ToString()))
                                        {
                                            if (!string.IsNullOrEmpty(numberUpc))
                                            {
                                                pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                fname.upc = "#" + numberUpc.Trim().ToLower();
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }
                                        else if (numberUpc.Count() < 7 && !Upcs.Contains(storeid.ToString()) && storeid == 11753)
                                        {
                                            var atl1 = dr["ALTUPC2"].ToString().ToLower();
                                            var atl2 = dr["ALTUPC1"].ToString().ToLower();
                                            if (string.IsNullOrEmpty(atl1))
                                                atl1 = "";
                                            if (string.IsNullOrEmpty(atl2))
                                                atl2 = "";
                                            string c = Regex.Replace(atl1, "[^0-9]", "");
                                            string d = Regex.Replace(atl2, "[^0-9]", "");

                                            if (c.Count() < 5 && !string.IsNullOrEmpty(c))
                                            {
                                                c = "11753" + c;
                                            }
                                            else if (d.Count() < 5 && !string.IsNullOrEmpty(d))
                                            {
                                                d = "11753" + d;
                                            }
                                            if (!string.IsNullOrEmpty(c) && !string.IsNullOrEmpty(d))
                                            {
                                                pmsk.upc = "#" + c;
                                                fname.upc = "#" + c;
                                            }
                                            else if (string.IsNullOrEmpty(c) && !string.IsNullOrEmpty(d))
                                            {
                                                pmsk.upc = "#" + d;
                                                fname.upc = "#" + d;
                                            }
                                            else if (!string.IsNullOrEmpty(c) && string.IsNullOrEmpty(d))
                                            {
                                                pmsk.upc = "#" + c;
                                                fname.upc = "#" + c;
                                            }
                                            else if (string.IsNullOrEmpty(c) && string.IsNullOrEmpty(d))
                                            {
                                                pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                fname.upc = "#" + numberUpc.Trim().ToLower();
                                            }
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                if (i == 1)
                                {
                                    pmsk.altupc1 = "#" + upcs[i]["upc"];
                                }
                                if (i == 2)
                                {
                                    pmsk.altupc2 = "#" + upcs[i]["upc"];
                                }
                                if (i == 3)
                                {
                                    pmsk.altupc3 = "#" + upcs[i]["upc"];
                                }
                                if (i == 4)
                                {
                                    pmsk.altupc4 = "#" + upcs[i]["upc"];
                                }
                                if (i == 5)
                                {
                                    pmsk.altupc5 = "#" + upcs[i]["upc"];
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(dr["sku"].ToString()))
                        {
                            pmsk.sku = "#" + dr["sku"].ToString();
                            fname.sku = "#" + dr["sku"].ToString();
                        }
                        else
                        { continue; }

                        pmsk.Qty = Convert.ToInt32(dr["Qty"]);

                        var productname = string.IsNullOrEmpty(dr["StoreProductName"].ToString()) ? "" : dr["StoreProductName"].ToString().Trim();
                        if (string.IsNullOrEmpty(productname))
                            productname = "";
                        pmsk.StoreProductName = productname;
                        pmsk.StoreDescription = productname;
                        fname.pdesc = productname;
                        fname.pname = productname;
                        if (IsMarkUpPrice) // Added as per ticket 15515 Store 11445
                        {
                            decimal price = Convert.ToDecimal(dr["Price"]);
                            decimal markup = price * MarkUpValue / 100 + price;
                            pmsk.Price = (markup);
                            pmsk.Price = Decimal.Round(pmsk.Price, 2);
                            fname.Price = Decimal.Round(pmsk.Price, 2);
                        }
                        else if (!string.IsNullOrEmpty(dr["Price"].ToString()))
                        {
                            pmsk.Price = Convert.ToDecimal(dr["Price"]);
                            fname.Price = Convert.ToDecimal(dr["Price"]);
                        }
                        if (pmsk.Price <= 0 || fname.Price <= 0)
                        {
                            continue;
                        }

                        if (altupc.Contains(storeid.ToString()))
                        {
                            pmsk.altupc1 = string.IsNullOrEmpty(dr["altupc1"].ToString()) ? "" : "#" + dr["altupc1"].ToString().Trim();
                            pmsk.altupc2 = string.IsNullOrEmpty(dr["altupc2"].ToString()) ? "" : "#" + dr["altupc2"].ToString().Trim();
                        }

                        string pak = dr.Field<string>("pack");
                        if (string.IsNullOrEmpty(pak))
                            pak = "1";
                        pmsk.pack = Regex.Replace(pak, @"[^0-9]", "").Trim();// Changed for #45262 (@"[^1-9]")
                        if (string.IsNullOrEmpty(pmsk.pack))
                            pmsk.pack = "1";
                        pmsk.Tax = Convert.ToDecimal(dr["Tax"]);
                        if (DifferentResponce.Contains(storeid.ToString()))
                        {
                            pmsk.sprice = Convert.ToDecimal(dr["sprice"]);
                        }
                        if (pmsk.sprice > 0 && DifferentResponce.Contains(storeid.ToString()))
                        {
                            pmsk.Start = DateTime.Now.AddDays(-1).ToString("MM/dd/yyyy");
                            pmsk.End = "12/31/2040";
                        }
                        else if (pmsk.sprice > 0 && !DifferentResponce.Contains(storeid.ToString()))
                        {
                            pmsk.Start = DateTime.Now.ToString("MM/dd/yyyy");
                            pmsk.End = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                        }
                        else
                        {
                            pmsk.Start = "";
                            pmsk.End = "";
                        }
                        fname.pcat = dr.Field<string>("pcat");
                        if (string.IsNullOrEmpty(fname.pcat))
                            fname.pcat = "";
                        if (storeid == 10350 && fname.pcat.ToLower().Contains("toba") || fname.pcat.ToLower().Contains("lighter")
                            || fname.pcat.ToLower().Contains("lotto"))
                        {
                            continue;
                        }
                        else if (storeid == 12234)
                        {
                            if ((fname.pcat.ToLower().Contains("toba") || fname.pcat.ToLower().Contains("lighter") ||
                                fname.pcat.ToLower().Contains("lotto")) && storeid == 12234)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if ((fname.pcat.ToLower().Contains("toba") || fname.pcat.ToLower().Contains("lighter") ||
                             fname.pcat.ToLower().Contains("cig") || fname.pcat.ToLower().Contains("lotto")) && storeid != 10708 && storeid != 10350)
                            {
                                continue;
                            }
                        }


                        fname.pcat1 = string.IsNullOrEmpty(dr["pcat1"].ToString()) ? "" : dr["pcat1"].ToString().Trim();
                        fname.pcat2 = "";
                        fname.pack = pmsk.pack;
                        pmsk.uom = string.IsNullOrEmpty(dr["uom"].ToString()) ? "" : dr["uom"].ToString().Trim();
                        if (Discount_SalePrice.Contains(storeid.ToString())) // As Per ticket 17401 Storeid : 10708
                        {
                            if (fname.pcat == "Wine" && pmsk.uom == "750ml" || pmsk.uom == "750")
                            {
                                decimal price = Convert.ToDecimal(dr["Price"]);
                                var ab = 8; // As per requirement price value multiply with 0.8 for Store 10708 ticket : 17401
                                decimal markup = price * ab / 10;
                                pmsk.sprice = Decimal.Round(markup, 2);
                                pmsk.Start = "10/16/2022";
                                pmsk.End = "10/22/2022";
                            }
                        }

                        if (BeerDeposit.Contains(storeid.ToString()) && pmsk.uom == "50ML")
                        {
                            continue;
                        }
                        fname.uom = pmsk.uom;
                        fname.region = "";
                        fname.country = "";
                        if (pmsk.pack == "1")
                        {
                            pmsk.pack = getpack(pmsk.StoreProductName).ToString();
                            fname.pack = pmsk.pack;
                        }
                        if (string.IsNullOrEmpty(pmsk.uom))
                        {
                            pmsk.uom = getVolume(pmsk.StoreProductName);
                            fname.uom = pmsk.uom;
                        }
                        if (storeid == 12485)//Added as per ticket #44322
                        {
                            var size = getVolume(pmsk.StoreProductName);
                            var name = pmsk.StoreProductName.ToUpper();
                            if (pmsk.uom.ToUpper() == "50ML" || Regex.IsMatch(size, @"^50ML") || pmsk.uom == ".050L")
                            {
                                pmsk.Deposit = Convert.ToInt32(pmsk.pack) * 0.05m;
                            }
                            else if (Regex.IsMatch(name, @"(\sCAN\s|\sBOTTLE\s)") || Regex.IsMatch(name, @"(\sCAN|\sCANS|\sBOTTLE|\sBOTTLES)$"))
                            {
                                pmsk.Deposit = Convert.ToInt32(pmsk.pack) * 0.10m;
                            }
                        }
                        if (BeerDeposit.Contains(storeid.ToString())) // Added for BeerDeposits values for ticket for store 11445
                        {
                            if (fname.pcat == "BEER" || fname.pcat == "SOFT DRINKS" || fname.pcat == "SOFT DRINKS" || fname.pcat == "JUICE" || fname.pcat == "CBD" || fname.pcat == "WATER")
                            {
                                double dbl = 0.05;
                                decimal dec = (decimal)dbl;
                                pmsk.Deposit = Convert.ToDecimal(pmsk.pack) * dec;
                            }
                        }
                        if (In_Stocks.Contains(storeid.ToString())) //storeid == 10826 || storeid == 11445
                        {
                            if (!string.IsNullOrEmpty(pmsk.upc) && pmsk.Qty > 0)
                            {
                                prodlist.Add(pmsk);
                                prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                full.Add(fname);
                                full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(pmsk.upc) && fname.pcat != "Cigarette" || fname.pcat != "Lotto" || fname.pcat != "Cigars" || fname.pcat != "TOBACCO" || fname.pcat != "CIG" || fname.pcat != "CIGARILLOS" || fname.pcat != "CIGAR" || fname.pcat != "CIGERATT" || fname.pcat != "CIGERATTE" || fname.pcat != "Tobacco")
                            {
                                if (IrrespectiveOfStock.Contains(storeid.ToString()) && !string.IsNullOrEmpty(pmsk.upc))
                                {
                                    prodlist.Add(pmsk);
                                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                    full.Add(fname);
                                    full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                }
                                else if (!IrrespectiveOfStock.Contains(storeid.ToString()) && !string.IsNullOrEmpty(pmsk.upc) && pmsk.Qty > 0)
                                {
                                    prodlist.Add(pmsk);
                                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                    full.Add(fname);
                                    full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                }
                            }
                        }
                    }
                    Console.WriteLine("Generating ADVENTPOS " + storeid + " Product CSV Files.....");
                    string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", storeid, BaseUrl);
                    Console.WriteLine("Product File Generated For ADVENTPOS " + storeid);
                }
                
                Console.WriteLine("Generating ADVENTPOS " + storeid + " Fullname CSV Files.....");
                string filename2 = GenerateCSV.GenerateCSVFile(full, "FULLNAME", storeid, BaseUrl);
                Console.WriteLine("Fullname File Generated For ADVENTPOS " + storeid);
            }
            catch (Exception ex)
            {
                Console.WriteLine("" + ex.Message + " For StoreId: "+ storeid);
                new Email().sendEmail(DeveloperId, "", "", "Error in Datatablecsv" + storeid + "AdventPOS@" + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
            }
        }
        public int getpack(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)PK");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                int outVal = 0;
                int.TryParse(prodPack.Replace("$", ""), out outVal);
                return outVal;
            }
            return 1;
        }
        public string getVolume(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)ML| (?<Result>\d+)LTR| (?<Result>\d+)OZ | (?<Result>\d+)L|(?<Result>\d+)OZ");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                return regexMatch.ToString();
            }
            return "";
        }
    }

    public class adventProductsModel  ///// For Mapping Tool of AdventPos
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
    public class ProductsModel12167
    {
        public int StoreID { get; set; }
        public string upc { get; set; }
        public Int64 Qty { get; set; }
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
        public decimal Deposit { get; set; }
        public decimal Discountable { get; set; }

    }
}

