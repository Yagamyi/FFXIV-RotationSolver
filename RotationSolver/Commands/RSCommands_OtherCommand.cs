﻿using ECommons.DalamudServices;
using RotationSolver.Localization;
using RotationSolver.Updaters;

namespace RotationSolver.Commands
{
    public static partial class RSCommands
    {
        private static void DoOtherCommand(OtherCommandType otherType, string str)
        {
            switch (otherType)
            {
                case OtherCommandType.Rotations:
                    var customCombo = RotationUpdater.RightNowRotation;
                    if (customCombo == null) return;

                    DoRotationCommand(customCombo, str);
                    break;

                case OtherCommandType.DoActions:
                    DoActionCommand(str);
                    break;

                case OtherCommandType.ToggleActions:
                    ToggleActionCommand(str);
                    break;

                case OtherCommandType.Settings:
                    DoSettingCommand(str);
                    break;
            }
        }

        private static void DoSettingCommand(string str)
        {
            if (!TryGetOneEnum<SettingsCommand>(str, out var type))
            {
                RotationSolverPlugin.OpenConfigWindow();
                return;
            }

            Basic.Configuration.PluginConfiguration.SetValue(type, !Basic.Configuration.PluginConfiguration.GetValue(type));

            //Say out.
            Svc.Chat.Print(string.Format(LocalizationManager.RightLang.Commands_ChangeSettingsValue,
                type.ToString(), Basic.Configuration.PluginConfiguration.GetValue(type)));
        }

        private static void ToggleActionCommand(string str)
        {
            foreach (var act in RotationUpdater.RightRotationActions)
            {
                if (str == act.Name)
                {
                    act.IsEnabled = !act.IsEnabled;

                    //Svc.Toasts.ShowQuest(string.Format(LocalizationManager.RightLang.Commands_InsertAction, time),
                    //    new Dalamud.Game.Gui.Toast.QuestToastOptions()
                    //    {
                    //        IconId = act.IconID,
                    //    });

                    return;
                }
            }
        }

        private static void DoActionCommand(string str)
        {
            //Todo!
            var strs = str.Split('-');

            if (strs != null && strs.Length == 2 && double.TryParse(strs[1], out var time))
            {
                var actName = strs[0];
                foreach (var iAct in RotationUpdater.RightRotationActions)
                {
                    if (iAct is IBaseAction act && !act.IsActionSequencer) continue;

                    if (actName == iAct.Name)
                    {
                        DataCenter.AddCommandAction(iAct, time);

                        Svc.Toasts.ShowQuest(string.Format(LocalizationManager.RightLang.Commands_InsertAction, time),
                            new Dalamud.Game.Gui.Toast.QuestToastOptions()
                            {
                                IconId = iAct.IconID,
                            });

                        return;
                    }
                }
            }

            Svc.Chat.PrintError(LocalizationManager.RightLang.Commands_InsertActionFailure);
        }


        private static void DoRotationCommand(ICustomRotation customCombo, string str)
        {
            var configs = customCombo.Configs;
            foreach (var config in configs)
            {
                if (config.DoCommand(configs, str))
                {
                    Svc.Chat.Print(string.Format(LocalizationManager.RightLang.Commands_ChangeRotationConfig,
                        config.DisplayName, configs.GetDisplayString(config.Name)));

                    return;
                }
            }
            Svc.Chat.Print(LocalizationManager.RightLang.Commands_CannotFindRotationConfig);
        }
    }
}
