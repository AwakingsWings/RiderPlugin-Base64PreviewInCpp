using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.UE4;
using System;

#nullable disable
namespace JetBrains.ReSharper.Feature.Services.Cpp.Daemon
{
    [DaemonStage(StagesBefore = new Type[] {typeof (CppIdentifierHighlightingStage), typeof (GlobalFileStructureCollectorStage)})]
    public class CppDaemonCustomStage : CppDaemonStageBase
    {
        private readonly ICppUE4SolutionDetector myUESolutionDetector;

        public CppDaemonCustomStage(
            ElementProblemAnalyzerRegistrar elementProblemAnalyzerRegistrar,
            ICppUE4SolutionDetector ueSolutionDetector)
            : base(elementProblemAnalyzerRegistrar)
        {
            this.myUESolutionDetector = ueSolutionDetector;
        }

        protected override IDaemonStageProcess CreateProcess(
            IDaemonProcess process,
            IContextBoundSettingsStore settings,
            DaemonProcessKind processKind,
            CppFile file)
        {
            return (IDaemonStageProcess) new CppFastDaemonStageCustomProcess(process, this.myElementProblemAnalyzerRegistrar, processKind, settings, file, this.myUESolutionDetector.IsUnrealSolution.Value, this.IsRadler);
        }

        protected override bool ShouldWorkInNonUserFile() => false;
    }
}