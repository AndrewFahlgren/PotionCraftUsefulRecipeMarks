using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Json
{
    public class BasicNamespaceMigration : INamespaceMigration
    {
        public string FromAssembly { get; set; }

        public string FromType { get; set; }

        public Type ToType { get; set; }
    }
}
