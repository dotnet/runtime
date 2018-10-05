using Mono.Cecil;

namespace Mono.Linker.Steps {
	public class PreserveCalendarsStep : IStep {
		readonly I18nAssemblies I18n;
		LinkContext context;

		public PreserveCalendarsStep (I18nAssemblies i18n)
		{
			I18n = i18n;
		}

		public virtual void Process (LinkContext context)
		{
			//
			// This step is mscorlib specific. We could convert this to complex
			// PreserveDependencyAttribute controlled by I18n but it's not worth it at this point
			//
			AssemblyDefinition mscorlib;
			if (!context.Resolver.AssemblyCache.TryGetValue ("mscorlib", out mscorlib))
				return;

			if (context.Annotations.GetAction (mscorlib) != AssemblyAction.Link)
				return;

			this.context = context;

			if (I18n.HasFlag (I18nAssemblies.MidEast)) {
				PreserveCalendar ("UmAlQuraCalendar", mscorlib);
				PreserveCalendar ("HijriCalendar", mscorlib);
				PreserveCalendar ("PersianCalendar", mscorlib);
			}

			if (I18n.HasFlag (I18nAssemblies.Other)) {
				PreserveCalendar ("ThaiBuddhistCalendar", mscorlib);
			}

			if (I18n.HasFlag (I18nAssemblies.CJK)) {
				PreserveCalendar ("ChineseLunisolarCalendar", mscorlib);
				PreserveCalendar ("JapaneseCalendar", mscorlib);
				PreserveCalendar ("JapaneseLunisolarCalendar", mscorlib);
				PreserveCalendar ("KoreanCalendar", mscorlib);
				PreserveCalendar ("KoreanLunisolarCalendar", mscorlib);
				PreserveCalendar ("TaiwanCalendar", mscorlib);
				PreserveCalendar ("TaiwanLunisolarCalendar", mscorlib);
			}

			if (I18n.HasFlag (I18nAssemblies.Rare)) {
				PreserveCalendar ("JulianCalendar", mscorlib);
			}
		}

		void PreserveCalendar (string name, AssemblyDefinition mscorlib)
		{
			var calendar = mscorlib.MainModule.GetType ("System.Globalization", name);
			if (calendar == null || !calendar.HasMethods)
				return;

			// we just preserve the default .ctor so Activation.Create will work, 
			// the normal linker logic will do the rest
			foreach (MethodDefinition ctor in calendar.Methods) {
				if (ctor.IsConstructor && !ctor.IsStatic && !ctor.HasParameters) {
					context.Annotations.AddPreservedMethod (calendar, ctor);
					// we need to mark the type or the above won't be processed
					context.Annotations.Mark (calendar);
					return;
				}
			}
		}
	}
}
