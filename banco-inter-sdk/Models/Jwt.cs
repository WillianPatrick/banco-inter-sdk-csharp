using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payments.Models
{
    internal class Jwt
    {
        public DateTime ExpiresOn { get; set; }
        public string Token { get; set; }
        public string[] Scopes { get; set; }

    }
}
