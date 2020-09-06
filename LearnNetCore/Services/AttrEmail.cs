using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearnNetCore.Services
{
    public class AttrEmail
    {
        public string mail = "jepri.tugas@gmail.com";
        public string pass = "indramayuplaza";
    }

    public class RandomDigit
    {
        private Random _random = new Random();
        public string GenerateRandom()
        {
            return _random.Next(0, 9999).ToString("D4");
        }
    }

}
