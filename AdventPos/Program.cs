using System;
using System.Configuration;


namespace AdventPos
{
    class Program
    {
        private static void Main(string[] args)
        {

            string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
            string FlatFile = ConfigurationManager.AppSettings["FlatFile"];
            try
            {
                POSSettings pOSSettings = new POSSettings();
                pOSSettings.IntializeStoreSettings();
                foreach (POSSetting current in pOSSettings.PosDetails)
                {
                    try
                    {
                        if (current.PosName.ToUpper() == "ADVENTPOS" && current.StoreSettings.POSSettings != null)
                        {
                            if (FlatFile.Contains(current.StoreSettings.StoreId.ToString()))
                            {
                                Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                                clsAdventPos_Flatfile advflat = new clsAdventPos_Flatfile(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.Upc);
                                Console.WriteLine();
                            }
                            else if(!string.IsNullOrEmpty(current.StoreSettings.POSSettings.BaseUrl))
                            {
                                Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                                CsvProducts clsAdventApi = new CsvProducts(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.Pin, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }//tested again for Advent git fail issues
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                new Email().sendEmail(DeveloperId, "", "", "Error in  Settings AdventPOS@" + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
            finally
            {
            }
        }
    }
}
