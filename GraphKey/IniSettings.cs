using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphKey
{
    public class IniSettings
    {
        public int RowCount { get; set; } = 5;
        public int ColumnCount { get; set; } = 8; 
        public List<UserInfo> UserBase { get; set; } = new List<UserInfo>();
    }
}
