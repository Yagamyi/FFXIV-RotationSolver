﻿using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;
using RotationSolver.Localization;
using RotationSolver.UI;

namespace RotationSolver.Updaters;

internal static class ActionUpdater
{
    static DateTime _startCombatTime = DateTime.MinValue;
    internal static DateTime _cancelTime = DateTime.MinValue;

    static  RandomDelay _GCDDelay = new(() => (Service.Config.WeaponDelayMin, Service.Config.WeaponDelayMax));

    internal static IAction NextAction { get; set; }
    internal static IBaseAction NextGCDAction { get; set; }

    internal static void UpdateNextAction()
    {
        PlayerCharacter localPlayer = Player.Object;
        if (localPlayer == null) return;

        try
        {
            var customRotation = RotationUpdater.RightNowRotation;

            if (customRotation?.TryInvoke(out var newAction, out var gcdAction) ?? false)
            {
                NextAction = newAction;

                if (gcdAction is IBaseAction GcdAction)
                {
                    if(NextGCDAction != GcdAction)
                    {
                        NextGCDAction = GcdAction;

                        var rightJobAndTarget = (Player.Object.IsJobCategory(JobRole.Tank) || Player.Object.IsJobCategory(JobRole.Melee)) && GcdAction.Target.IsNPCEnemy();

                        if (Service.Config.DrawPositional && rightJobAndTarget && GcdAction.IsSingleTarget)
                        {
                            PainterManager.UpdatePositional(GcdAction.EnemyPositional, GcdAction.Target);
                        }
                        else
                        {
                            PainterManager.ClearPositional();
                        }

                        if (GcdAction.EnemyPositional != EnemyPositional.None 
                            && GcdAction.Target.HasPositional()
                            && !localPlayer.HasStatus(true, CustomRotation.TrueNorth.StatusProvide))
                        {

                            if (CheckAction())
                            {
                                string positional = GcdAction.EnemyPositional.ToName();
                                if (Service.Config.SayPositional) SpeechHelper.Speak(positional);
                                if (Service.Config.ToastPositional) Svc.Toasts.ShowQuest(" " + positional,
                                    new Dalamud.Game.Gui.Toast.QuestToastOptions()
                                {
                                    IconId = GcdAction.IconID,
                                });
                            }
                        }
                    }
                }
                else
                {
                    NextGCDAction = null;
                    PainterManager.ClearPositional();
                }
                return;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to update next action.");
        }

        NextAction = NextGCDAction = null;
        PainterManager.ClearPositional();
    }

    static DateTime lastTime;
    static bool CheckAction()
    {
        if (DateTime.Now - lastTime > new TimeSpan(0, 0, 3) && DataCenter.StateType != StateCommandType.Cancel)
        {
            lastTime = DateTime.Now;
            return true;
        }
        else return false;
    }
    internal unsafe static void UpdateActionInfo()
    {
        UpdateWeaponTime();
        UpdateTimeInfo();
    }

    private unsafe static void UpdateTimeInfo()
    {
        var last = DataCenter.InCombat;
        DataCenter.InCombat = Svc.Condition[ConditionFlag.InCombat];
        if(!last && DataCenter.InCombat)
        {
            _startCombatTime = DateTime.Now;
        }
        else if(last && !DataCenter.InCombat)
        {
            _startCombatTime = DateTime.MinValue;
            if (Service.Config.AutoOffAfterCombat > 0)
            {
                _cancelTime = DateTime.Now.AddSeconds(Service.Config.AutoOffAfterCombat);
            }
        }
        if (_startCombatTime == DateTime.MinValue)
        {
            DataCenter.CombatTime = 0;
        }
        else
        {
            DataCenter.CombatTime = (float)(DateTime.Now - _startCombatTime).TotalSeconds;
        }

        for (int i = 0; i < DataCenter.BluSlots.Length; i++)
        {
            DataCenter.BluSlots[i] = ActionManager.Instance()->GetActiveBlueMageActionInSlot(i);
        }
        UpdateMPTimer();
    }

    private static unsafe void UpdateWeaponTime()
    {
        var player = Player.Object;
        if (player == null) return;

        var instance = ActionManager.Instance();

        var castTotal = player.TotalCastTime;

        var weaponTotal = instance->GetRecastTime(ActionType.Spell, 11);
        if (castTotal > 0) castTotal += 0.1f;
        if (player.IsCasting) weaponTotal = Math.Max(castTotal, weaponTotal);

        DataCenter.WeaponElapsed = instance->GetRecastTimeElapsed(ActionType.Spell, 11);
        DataCenter.WeaponRemain = DataCenter.WeaponElapsed == 0 ? player.TotalCastTime - player.CurrentCastTime
            : Math.Max(weaponTotal - DataCenter.WeaponElapsed, player.TotalCastTime - player.CurrentCastTime);

        //Casting time.
        if (DataCenter.WeaponElapsed < 0.3) DataCenter.CastingTotal = castTotal;
        if (weaponTotal > 0 && DataCenter.WeaponElapsed > 0.2) DataCenter.WeaponTotal = weaponTotal;
    }

    static uint _lastMP = 0;
    static DateTime _lastMPUpdate = DateTime.Now;
    /// <summary>
    /// 跳蓝经过时间
    /// </summary>
    internal static float MPUpdateElapsed => (float)(DateTime.Now - _lastMPUpdate).TotalSeconds % 3;

    private static void UpdateMPTimer()
    {
        var player = Player.Object;
        if (player == null) return;

        //不是黑魔不考虑啊
        if (player.ClassJob.Id != (uint)ECommons.ExcelServices.Job.BLM) return;

        //有醒梦，就算了啊
        if (player.HasStatus(true, StatusID.LucidDreaming)) return;

        if (_lastMP < player.CurrentMp)
        {
            _lastMPUpdate = DateTime.Now;
        }
        _lastMP = player.CurrentMp;
    }

    internal unsafe static void DoAction()
    {
        if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
            || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            || Svc.Condition[ConditionFlag.Occupied33]
            || Svc.Condition[ConditionFlag.Occupied38]
            || Svc.Condition[ConditionFlag.Jumping61]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51]
            || Svc.Condition[ConditionFlag.Mounted]
            //|| Svc.Condition[ConditionFlag.SufferingStatusAffliction] //Because of BLU30!
            || Svc.Condition[ConditionFlag.SufferingStatusAffliction2]
            || Svc.Condition[ConditionFlag.RolePlaying]
            || Svc.Condition[ConditionFlag.InFlight]
            ||  ActionManager.Instance()->ActionQueued && NextAction != null
                && ActionManager.Instance()->QueuedActionId != NextAction.AdjustedID
            || Player.Object.CurrentHp == 0) return;

        var maxAhead = Math.Max(DataCenter.MinAnimationLock - DataCenter.Ping, 0.08f);
        var ahead = Math.Min(maxAhead, Service.Config.ActionAhead);

        //GCD
        var canUseGCD = DataCenter.WeaponRemain <= ahead;
        if (_GCDDelay.Delay(canUseGCD)) RSCommands.DoAnAction(true);
        if (canUseGCD) return;

        var nextAction = NextAction;
        if (nextAction == null) return;

        var timeToNext = DataCenter.ActionRemain;

        //No time to use 0gcd
        if (timeToNext + nextAction.AnimationLockTime
            > DataCenter.WeaponRemain) return;

        //Skip when casting
        if (DataCenter.WeaponElapsed <= DataCenter.CastingTotal) return;

        //The last one.
        if (timeToNext + nextAction.AnimationLockTime + DataCenter.Ping + DataCenter.MinAnimationLock > DataCenter.WeaponRemain)
        {
            if (DataCenter.WeaponRemain > nextAction.AnimationLockTime + ahead +
                Math.Max(DataCenter.Ping, Service.Config.MinLastAbilityAdvanced)) return;

            RSCommands.DoAnAction(false);
        }
        else if (timeToNext < ahead)
        {
            RSCommands.DoAnAction(false);
        }
    }
}
