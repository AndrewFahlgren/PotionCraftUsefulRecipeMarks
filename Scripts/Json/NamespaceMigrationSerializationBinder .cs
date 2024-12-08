using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Json
{
    public class NamespaceMigrationSerializationBinder(params INamespaceMigration[] migrations) : DefaultSerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var migration = migrations.SingleOrDefault(p => p.FromAssembly == assemblyName && p.FromType == typeName);
            if (migration != null)
            {
                return migration.ToType;
            }
            return base.BindToType(assemblyName, typeName);
        }
    }
}
