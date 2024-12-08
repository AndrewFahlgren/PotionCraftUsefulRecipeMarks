using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Json
{
    public interface INamespaceMigration
    {
        string FromAssembly { get; }

        string FromType { get; }

        Type ToType { get; }
    }
}
