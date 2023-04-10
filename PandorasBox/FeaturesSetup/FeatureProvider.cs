using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features
{
    public class FeatureProvider : IDisposable
    {
        public bool Disposed { get; protected set; } = false;

        public List<BaseFeature> Features { get; } = new();

        public Assembly Assembly { get; init; } = null!;

        public FeatureProvider(Assembly assembly)
        {
            Assembly = assembly;
        }

        public virtual void LoadFeatures()
        {
            foreach (var t in Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Feature))&& !x.IsAbstract))
            {
                try
                {
                    var feature = (Feature)Activator.CreateInstance(t);
                    feature.InterfaceSetup(P, pi, Config, this);
                    feature.Setup();
                    if (feature.Ready && Config.EnabledFeatures.Contains(t.Name))
                    {
                        feature.Enable();
                    }

                    Features.Add(feature);
                }
                catch (Exception ex) 
                {
                    PluginLog.Error(ex, $"Feature not loaded: {t.Name}");
                }
            }
        }

        public void UnloadFeatures()
        {
            foreach (var t in Features)
            {
                if (t.Enabled)
                {
                    try
                    {
                        t.Disable();
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, $"Cannot disable {t.Name}");
                    }
                }
            }
            Features.Clear();
        }

        public void Dispose()
        {
            UnloadFeatures();
            Disposed = true;
        }
    }
}