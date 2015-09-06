using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kyiv_Bomb_Shelters
{
    class Shelter
    {
        string adress = "";

        public string Adress
        {
            get { return adress; }
            set { adress = value; }
        }
        GeoCoordinate coordinate = new GeoCoordinate();

        public GeoCoordinate Coordinate
        {
            get { return coordinate; }
            set { coordinate = value; }
        }
        string type = "";

        public string Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}
