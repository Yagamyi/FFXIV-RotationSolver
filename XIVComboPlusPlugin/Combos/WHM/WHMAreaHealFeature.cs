﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XIVComboPlus.Combos.WHM
{
    internal class WHMAreaHealFeature : WHMCombo
    {
        public override string ComboFancyName => "白魔群奶";

        public override string Description => "替换医治为群体奶";

        protected internal override uint[] ActionIDs => new uint[] {Actions.Medica.ActionID};

        protected override uint Invoke(uint actionID, uint lastComboActionID, float comboTime, byte level)
        {
            uint act;

            //有人搞超火！这很刺激！
            if (UseBenediction(out _) && Actions.Benediction.TryUseAction(level, out act, mustUse: true)) return act;

            //if (TargetHelper.DeathPeopleAll.Length != 0)
            //{
            //    //如果有人倒了，不管怎么样，赶紧拉！
            //    if (Actions.Raise.TryUseAction(level, out act, mustUse: true)) return act;
            //}

            if (CanInsertAbility)
            {
                //法令
                if (Actions.Assize.TryUseAction(level, out act, mustUse: true)) return act;

                //庇护所
                if (Actions.Asylum.TryUseAction(level, out act, mustUse: true)) return act;

                //礼仪之铃
                if (Actions.LiturgyoftheBell.TryUseAction(level, out act, mustUse: true)) return act;
            }

            //如果现在可以增加能力技
            if (CanAddAbility(level, out act)) return act;

            if (IsMoving)
            {
                //狂喜之心
                if (Actions.AfflatusRapture.TryUseAction(level, out act, mustUse: true)) return act;
            }

            //狂喜之心
            if (Actions.AfflatusRapture.TryUseAction(level, out act)) return act;
            //加Hot
            if (Actions.Medica2.TryUseAction(level, out act)) return act;

            float cure3 = TargetHelper.GetBestHeal(Actions.Cure3.Action, 600);
            float medica = TargetHelper.GetBestHeal(Actions.Medica.Action, 300);

            //愈疗
            if (cure3 > medica && Actions.Cure3.TryUseAction(level, out act)) return act;
            if (Actions.Medica.TryUseAction(level, out act)) return act;


            return Actions.AfflatusRapture.ActionID;
        }

    }
}
