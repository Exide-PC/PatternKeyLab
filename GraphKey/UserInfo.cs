using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphKey
{
    public class UserInfo
    {
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public bool IsSuperUser { get; set; } = false;
    }
}
