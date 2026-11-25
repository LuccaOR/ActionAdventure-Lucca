using System;
using System.Reflection;
using UnityEditor;

namespace SlimeHunter.EditorTools
{
    /// <summary>Atalhos de desenvolvimento usados pela automação (Unity CLI).</summary>
    public static class DevTools
    {
        /// <summary>
        /// Recarrega o domínio de scripts sem recompilar. Descarrega os assemblies em memória que o
        /// `unity command script run` cria (bug do Pipeline no Unity 6.6: depois do primeiro run,
        /// os próximos falham com "String conversion error").
        /// </summary>
        [MenuItem("Slime Hunter/Dev/Recarregar Dominio")]
        public static void ReloadDomain() => EditorUtility.RequestScriptReload();

        /// <summary>
        /// Desliga só a coleta de detalhes de eval da telemetria do pacote Pipeline. Ela falha em
        /// caminhos com acento ("Área de Trabalho") e enche o console com o aviso
        /// "Pipeline analytics failed to record an eval's compile". Não muda as configurações de
        /// analytics do Unity nem afeta os comandos. Roda depois da inicialização do pacote.
        /// </summary>
        [InitializeOnLoadMethod]
        static void DisablePipelineEvalAnalytics()
        {
            // O pacote liga a coleta no InitializeOnLoad dele, que pode rodar depois do nosso:
            // aplicamos de novo quando todos os inicializadores terminaram.
            AssemblyReloadEvents.afterAssemblyReload += ApplyEvalAnalyticsOff;
            EditorApplication.delayCall += ApplyEvalAnalyticsOff;
            ApplyEvalAnalyticsOff();
        }

        static void ApplyEvalAnalyticsOff()
        {
            var type = Type.GetType("Unity.Pipeline.Telemetry.EvalAnalyticsDetails, Unity.Pipeline");
            var prop = type?.GetProperty("IsCollecting", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite) prop.SetValue(null, (Func<bool>)(() => false));
        }
    }
}
