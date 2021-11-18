using DataAccessLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; 

namespace ParserWoodDeals
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start getting data...");
            ParseAndSaveDataToDB();
            Console.ReadLine();
        }

        static void ParseAndSaveDataToDB()
        {
            try
            {
                var dal = new DAL();
                int dealsTotalCount = GetTotalDealsCount();
                Console.WriteLine("Total amount of deals on portal: {0}", dealsTotalCount);
                int dealsCountInDB = dal.GetDealsCount();
                Console.WriteLine("Total amount of deals in DB: {0}", dealsCountInDB);

                //check for new deals
                if (dealsTotalCount > dealsCountInDB)
                {
                    Console.WriteLine("Start getting new data from portal...");
                    var dealNumberFromDB = dal.GetDealNumbers();
                    // data for main request

                    //do request for 100 deals to prevent stackoverflow
                    for (int i = 0; i < dealsTotalCount / 100; i++)
                    {
                        string querySearchReportWoodDeal = "{\"query\":\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {\\n  " +
                            "searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {\\n    content {\\n      " +
                            "sellerName\\n      sellerInn\\n      buyerName\\n      buyerInn\\n      woodVolumeBuyer\\n      woodVolumeSeller\\n      dealDate\\n      " +
                            "dealNumber\\n      __typename\\n    }\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":" + 100 +
                            ",\"number\":" + i +
                            ",\"filter\":null,\"orders\":null}," +
                            "\"operationName\":\"SearchReportWoodDeal\"}";
                        string data = RequestDataFromPortal(querySearchReportWoodDeal);
                        var json = data.Substring(data.IndexOf("content") + 9, data.Length - data.IndexOf("content") - 46);

                        //parse json and save to db
                        var doc = JsonDocument.Parse(json);
                        JsonElement root = doc.RootElement;

                        var deals = root.EnumerateArray();
                        while (deals.MoveNext())
                        {
                            var deal = deals.Current;
                            var props = deal.EnumerateObject();

                            while (props.MoveNext())
                            {
                                var prop = props.Current;
                                if (prop.Name == "dealNumber")
                                {
                                    decimal dealNumber = Convert.ToDecimal(prop.Value.ToString());
                                    if (dealNumberFromDB.Where(n => n == Convert.ToDecimal(prop.Value.ToString())).Count() == 0)
                                    {
                                        dal.AddDeal(new WoodDeal() { DealNumber = dealNumber, DealData = deal.ToString() });
                                        Console.WriteLine("Deal with number {0} has been added to DB", dealNumber);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Deal with number {0} is already in DB", dealNumber);
                                    }
                                }
                            }
                        } }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }
        }

        static string RequestDataFromPortal(string query)
        {
            Console.WriteLine("Request data from portal...");
            WebRequest request = WebRequest.Create("https://www.lesegais.ru/open-area/graphql");
            request.Method = "POST"; // для отправки используется метод Post
            request.Headers["sec-fetch-mode"] = "cors";
            request.Headers["sec-fetch-site"] = "same-origin";
            // устанавливаем тип содержимого - параметр ContentType
            request.ContentType = "application/json";
            // Устанавливаем заголовок Content-Length запроса - свойство ContentLength
            request.ContentLength = query.Length;

            //записываем данные в поток запроса
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            { 
                streamWriter.Write(query);
            }

            WebResponse response = request.GetResponse();
            string requestedData;
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    //Console.WriteLine(reader.ReadToEnd());
                    requestedData = reader.ReadToEnd();
                }
            }
            response.Close();
            Console.WriteLine("Data has been requested." );
            return requestedData;
        }

        static int GetTotalDealsCount()
        {
            string queryDealCount = "{\"query\":\"query SearchReportWoodDealCount($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {\\n  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {\\n    total\\n    number\\n    size\\n    overallBuyerVolume\\n    overallSellerVolume\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":20,\"number\":0,\"filter\":null},\"operationName\":\"SearchReportWoodDealCount\"}";
            string data = RequestDataFromPortal(queryDealCount);

            int totalEnd = data.IndexOf("number") - 2;
            int totalStart = data.IndexOf("total") + 7;
            var totalDealsCout = Convert.ToInt32(data.Substring(totalStart, totalEnd - totalStart));
            return totalDealsCout;
        }
    }
}
