namespace Thunder.Md.CodeExtensions;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.Extensions.Config;

public static class PdfStyling{
    extension(IContainer container){
        public IContainer AddBorderRadius(ThunderConfig thunderConfig){
            if(thunderConfig.Project is null){
                return container;
            }

            return container.CornerRadius(0.5f, Unit.Millimetre); //TODO: Add to config
        }
    }
}