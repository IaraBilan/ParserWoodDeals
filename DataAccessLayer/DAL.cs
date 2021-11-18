using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer
{
    public class DAL
    {
        readonly ParserEntities db = new ParserEntities();

        public int GetDealsCount()
        {
            try
            {
                return db.WoodDeals.ToList().Count();
            }
            catch
            {
                throw;
            }
        }

        public IEnumerable<Decimal> GetDealNumbers()
        {
            try
            {

                return from r in db.WoodDeals
                       select r.DealNumber;
            }
            catch
            {
                throw;
            }
        }


        //To Add new deal
        public int AddDeal(WoodDeal deal)
        {
            try
            {
                var obj = db.WoodDeals.SingleOrDefault(d => d.DealNumber == deal.DealNumber);

                if (obj == null)
                {
                    db.WoodDeals.Add(deal);
                    db.SaveChanges();
                }

                return 1;
            }
            catch
            {
                throw;
            }
        }
    }
}
