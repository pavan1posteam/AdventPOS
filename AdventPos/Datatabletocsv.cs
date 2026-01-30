using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdventPos
{
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
                DataTable dt = ToDataTable(dtlist, storeid);
                var dtr = from s in dt.AsEnumerable() select s;
                List<ProductsModel> prodlist = new List<ProductsModel>();
                List<FullNameProductModel> full = new List<FullNameProductModel>();
                var exclude = new List<string>();
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
                        pmsk.Discountable = Convert.ToInt32(dr["discountable"]);//Added by PK on 30-01-2026
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
                    if (prodlist.Count > 0)//Added by PK on 30-01-2026
                    {
                        bool chkDiscountable = prodlist.Any(x => x.Discountable == 1);
                        if (!chkDiscountable)
                        {
                            exclude.Add("Discountable");
                        }
                    }
                    Console.WriteLine("Generating ADVENTPOS " + storeid + " Product CSV Files.....");
                    string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", storeid, BaseUrl, exclude);//Changed by PK on 30-01-2026
                    Console.WriteLine("Product File Generated For ADVENTPOS " + storeid);
                }

                Console.WriteLine("Generating ADVENTPOS " + storeid + " Fullname CSV Files.....");
                string filename2 = GenerateCSV.GenerateCSVFile(full, "FULLNAME", storeid, BaseUrl);
                Console.WriteLine("Fullname File Generated For ADVENTPOS " + storeid);
            }
            catch (Exception ex)
            {
                Console.WriteLine("" + ex.Message + " For StoreId: " + storeid);
                new Email().sendEmail(DeveloperId, "", "", "Error in Datatablecsv" + storeid + "AdventPOS@" + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
            }
        }
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
