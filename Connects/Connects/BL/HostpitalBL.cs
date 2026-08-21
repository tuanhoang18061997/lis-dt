using Connects.Models;
using System;
using System.Linq;

namespace Connects.BL
{
    public class HostpitalBL
    {
        private static LABContext db;

        public static Hospital Get_Hostpital()
        {
            db = new LABContext();
            return db.Hospitals.FirstOrDefault();
        }
    }
}
