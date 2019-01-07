using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Twoxzi.TeamViewerManager
{
    [AttributeUsage(AttributeTargets.Property,AllowMultiple =false)]
   public class TeamviewerKeyAttribute:Attribute
    {
        public String KeyName { get; set; }
        public TeamviewerKeyAttribute(String keyName)
        {
            KeyName = keyName;
        }
    }
}
