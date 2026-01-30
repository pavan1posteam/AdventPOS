
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
        string prcFromPrcA = ConfigurationManager.AppSettings["PriceFromPriceA"];
        string DifferentResponce = ConfigurationManager.AppSettings["DifferentResponce"];
        string staticQtyStores = ConfigurationManager.AppSettings["StaticQtyStores"];
        public clsAdventPos(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            productForCSV(storeid, tax, BaseUrl, Username, Password, Pin, IsMarkUpPrice, MarkUpValue);
        }
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
                //File.WriteAllText($"{StoreId}.json",response.Content);
               
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
        public void productForCSV(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            try
            {
                var productList = products(storeid, tax, BaseUrl, Username, Password, Pin);

                BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");

                List<datatableModel> pf = new List<datatableModel>();
                if (productList != null)
                {
                    if (productList.Count > 0)
                    {
                        bool chkDiscountable = productList.All(x => x.IsNonDiscountable == 0);
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
                            #region old qty logic 
                            //decimal qty = Convert.ToDecimal(item.TotalQty);
                            //pdf.Qty = Convert.ToInt32(qty) > 0 ? Convert.ToInt32(qty) : 0;
                            #endregion


                            //new quantity logic 
                            int quantity = Convert.ToInt32(item.TotalQty);

                            if (staticQtyStores.Contains(storeid.ToString()))
                            {
                                if (storeid == 12710)
                                {
                                    // if qty <= 0 → static 999
                                    // if qty > 0 → actual qty
                                    pdf.Qty = quantity <= 0 ? 999 : quantity;
                                }
                                else
                                {
                                    // for all other stores in static config
                                    pdf.Qty = 999;
                                }
                            }
                            else
                            {
                                // normal behavior
                                pdf.Qty = quantity > 0 ? quantity : 0;
                            }

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

                            //Added by PK on 30-01-2026
                            if (chkDiscountable)//By checking: discountable column will be removed from csv 
                                pdf.discountable = 0;
                            else
                                pdf.discountable = item.IsNonDiscountable == 1? 0: 1;
                            //End
                            pf.Add(pdf);
                        }
                        
                        Datatabletocsv csv = new Datatabletocsv();
                        csv.Datatablecsv(storeid, tax, pf, IsMarkUpPrice, MarkUpValue);
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
            [JsonProperty("sku")]
            public int SKU { get; set; }
            [JsonProperty("itemname")]
            public string ItemName { get; set; }
            [JsonProperty("price")]
            public double Price { get; set; }
            [JsonProperty("cost")]
            public double Cost { get; set; }
            [JsonProperty("saleprice")]
            public double SALEPRICE { get; set; }
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
            public string ALTUPC1 { get; set; }
            [JsonProperty("altupc2")]
            public string ALTUPC2 { get; set; }
            [JsonProperty("storecode")]
            public int STORECODE { get; set; }
        }

        public class items
        {
            public List<Data> item { get; set; }
        }
    }
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
        [JsonProperty("isnondiscountable")]
        public int IsNonDiscountable { get; set; } = 0;

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
        public int discountable { get; set; }
    }
}

