﻿// <copyright file="AbilityExtensions.cs" company="EnsageSharp">
//    Copyright (c) 2015 EnsageSharp.
// 
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
// 
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
// 
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see http://www.gnu.org/licenses/
// </copyright>

namespace Ensage.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ensage.Common.AbilityInfo;

    using global::SharpDX;

    /// <summary>
    /// </summary>
    public static class AbilityExtensions
    {
        #region Static Fields

        /// <summary>
        ///     Temporarily stores cast delay values
        /// </summary>
        public static Dictionary<string, double> DelayDictionary = new Dictionary<string, double>();

        /// <summary>
        ///     Temporarily stores radius values
        /// </summary>
        public static Dictionary<string, float> RadiusDictionary = new Dictionary<string, float>();

        /// <summary>
        ///     Temporarily stores speed values
        /// </summary>
        public static Dictionary<string, float> SpeedDictionary = new Dictionary<string, float>();

        private static readonly Dictionary<string, AbilityBehavior> AbilityBehaviorDictionary =
            new Dictionary<string, AbilityBehavior>();

        private static readonly Dictionary<string, bool> BoolDictionary = new Dictionary<string, bool>();

        private static readonly Dictionary<string, double> CastPointDictionary = new Dictionary<string, double>();

        private static readonly Dictionary<string, AbilityData> DataDictionary = new Dictionary<string, AbilityData>();

        private static readonly Dictionary<string, float> ChannelDictionary = new Dictionary<string, float>();

        #endregion

        #region Public Methods and Operators


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="type"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool IsAbilityType(this Ability ability, AbilityType type, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            var n = name + "abilityType" + type.ToString();
            if (BoolDictionary.ContainsKey(n))
            {
                return BoolDictionary[n];
            }
            var value = ability.AbilityType == type;
            BoolDictionary.Add(n, value);
            return value;
        }
        /// <summary>
        ///     Checks if given ability can be used
        /// </summary>
        /// <param name="ability"></param>
        /// <returns>returns true in case ability can be used</returns>
        public static bool CanBeCasted(this Ability ability)
        {
            if (ability == null)
            {
                return false;
            }
            var dictiKey = ability.Handle + "CanBeCasted";
            if (!Utils.SleepCheck(dictiKey))
            {
                return BoolDictionary[dictiKey];
            }
            try
            {
                var owner = ability.Owner as Hero;
                bool canBeCasted;
                if (!BoolDictionary.ContainsKey(dictiKey))
                {
                    BoolDictionary.Add(dictiKey, false);
                }
                if (owner == null)
                {
                    canBeCasted = ability.Level > 0 && ability.Cooldown <= 0;
                    var item = ability as Item;
                    if (item != null && item.IsRequiringCharges)
                    {
                        canBeCasted = canBeCasted && item.CurrentCharges > 0;
                    }
                    BoolDictionary[dictiKey] = canBeCasted;
                    Utils.Sleep(100, dictiKey);
                    return canBeCasted;
                }
                if (ability is Item || owner.ClassID != ClassID.CDOTA_Unit_Hero_Invoker)
                {
                    canBeCasted = ability.AbilityState == AbilityState.Ready && ability.Level > 0;
                    var item = ability as Item;
                    if (item != null && item.IsRequiringCharges)
                    {
                        canBeCasted = canBeCasted && item.CurrentCharges > 0;
                    }
                    BoolDictionary[dictiKey] = canBeCasted;
                    Utils.Sleep(100, dictiKey);
                    return canBeCasted;
                }
                var name = ability.Name;
                if (name != "invoker_invoke" && name != "invoker_quas" && name != "invoker_wex"
                    && name != "invoker_exort" && ability.AbilitySlot != AbilitySlot.Slot_4
                    && ability.AbilitySlot != AbilitySlot.Slot_5)
                {
                    BoolDictionary[dictiKey] = false;
                    Utils.Sleep(100, dictiKey);
                    return false;
                }
                canBeCasted = ability.AbilityState == AbilityState.Ready && ability.Level > 0;
                BoolDictionary[dictiKey] = canBeCasted;
                Utils.Sleep(100, dictiKey);
                return canBeCasted;
            }
            catch (Exception)
            {
                // Console.WriteLine(e.GetBaseException());
                return false;
            }
        }

        /// <summary>
        ///     Checks if given ability can be used
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <returns>returns true in case ability can be used</returns>
        public static bool CanBeCasted(this Ability ability, Unit target)
        {
            if (!target.IsValidTarget())
            {
                return false;
            }

            var canBeCasted = ability.CanBeCasted();
            if (!target.IsMagicImmune())
            {
                return canBeCasted;
            }

            AbilityInfo data;
            if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
            {
                data = AbilityDatabase.Find(ability.Name);
                AbilityDamage.DataDictionary.Add(ability, data);
            }
            return data == null ? canBeCasted : data.MagicImmunityPierce;
        }

        /// <summary>
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool CanHit(this Ability ability, Hero target, string abilityName = null)
        {
            return CanHit(ability, target, ability.Owner.Position, abilityName);
        }

        /// <summary>
        ///     Checks if you could hit hero with given ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="sourcePosition"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool CanHit(this Ability ability, Hero target, Vector3 sourcePosition, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            if (ability.Owner.Equals(target))
            {
                return true;
            }
            var position = sourcePosition;
            if (ability.IsAbilityBehavior(AbilityBehavior.Point, name)
                || ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name))
            {
                var pred = ability.GetPrediction(target, abilityName: name);
                if (position.Distance2D(pred) <= (ability.GetCastRange(name)))
                {
                    return true;
                }
                return false;
            }
            if (ability.IsAbilityBehavior(AbilityBehavior.UnitTarget, name))
            {
                if (position.Distance2D(target.Position) <= ability.GetCastRange(name))
                {
                    return true;
                }
                if (name == "pudge_dismember" && target.Modifiers.Any(x => x.Name == "modifier_pudge_meat_hook")
                    && position.Distance2D(target) < 600)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool CastSkillShot(this Ability ability, Unit target, string abilityName = null)
        {
            return CastSkillShot(ability, target, ability.Owner.Position, abilityName);
        }

        /// <summary>
        ///     Uses prediction to cast given skillshot ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="sourcePosition"></param>
        /// <param name="abilityName"></param>
        /// <returns>returns true in case of successfull cast</returns>
        public static bool CastSkillShot(
            this Ability ability,
            Unit target,
            Vector3 sourcePosition,
            string abilityName = null)
        {
            if (!Utils.SleepCheck("CastSkillshot" + ability.Handle))
            {
                return false;
            }
            var name = abilityName ?? ability.Name;
            var owner = ability.Owner as Unit;
            var position = sourcePosition;
            var delay = ability.GetHitDelay(target, name);
            //AbilityInfo data;
            //if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
            //{
            //    data = AbilityDatabase.Find(ability.Name);
            //    AbilityDamage.DataDictionary.Add(ability, data);
            //}
            //delay += data.AdditionalDelay;
            if (target.IsInvul() && !Utils.ChainStun(target, delay, null, false))
            {
                return false;
            }
            var xyz = ability.GetPrediction(target, abilityName: name);
            var radius = ability.GetRadius(name);
            var speed = ability.GetProjectileSpeed(name);
            var distanceXyz = xyz.Distance2D(position);
            var range = ability.GetCastRange(name);
            if (!(distanceXyz <= (range + radius)))
            {
                return false;
            }
            if (distanceXyz > range)
            {
                xyz = (position - xyz) * range / distanceXyz + xyz;
            }
            // Console.WriteLine(ability.GetCastRange() + " " + radius);
            if (name.Substring(0, Math.Min("nevermore_shadowraze".Length, name.Length)) == "nevermore_shadowraze")
            {
                xyz = Prediction.SkillShotXYZ(
                    owner,
                    target,
                    (float)((delay + (float)owner.GetTurnTime(xyz)) * 1000),
                    speed,
                    radius);
                if (distanceXyz < (range + radius) && distanceXyz > (range - radius))
                {
                    if (owner.GetTurnTime(xyz) > 0.01)
                    {
                        owner.Move((position - xyz) * 50 / position.Distance2D(xyz) + xyz);
                    }
                    else
                    {
                        ability.UseAbility();
                    }
                    return true;
                }
                return false;
            }
            if (name == "invoker_ice_wall" && distanceXyz - 50 > 200 && distanceXyz - 50 < 610)
            {
                var mepred = (position - target.Position) * 50 / position.Distance2D(target) + target.Position;
                var v1 = xyz.X - mepred.X;
                var v2 = xyz.Y - mepred.Y;
                var a = Math.Acos(175 / xyz.Distance(mepred));
                var x1 = v1 * Math.Cos(a) - v2 * Math.Sin(a);
                var y1 = v2 * Math.Cos(a) + v1 * Math.Sin(a);
                var b = Math.Sqrt((x1 * x1) + (y1 * y1));
                var k1 = x1 * 50 / b;
                var k2 = y1 * 50 / b;
                var vec1 = new Vector3((float)(k1 + mepred.X), (float)(k2 + mepred.Y), mepred.Z);
                if (vec1.Distance2D(mepred) > 0)
                {
                    owner.Move(mepred);
                    owner.Move(vec1, true);
                    ability.UseAbility(true);

                    return true;
                }
                return false;
            }
            ability.UseAbility(xyz);
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="straightTimeforSkillShot"></param>
        /// <param name="chainStun"></param>
        /// <param name="useSleep"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool CastStun(
            this Ability ability,
            Unit target,
            float straightTimeforSkillShot = 0,
            bool chainStun = true,
            bool useSleep = true,
            string abilityName = null)
        {
            return CastStun(
                ability,
                target,
                ability.Owner.Position,
                straightTimeforSkillShot,
                chainStun,
                useSleep,
                abilityName);
        }

        /// <summary>
        ///     Uses given ability in case enemy is not disabled or would be chain stunned.
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="sourcePosition"></param>
        /// <param name="straightTimeforSkillShot"></param>
        /// <param name="chainStun"></param>
        /// <param name="useSleep"></param>
        /// <param name="abilityName"></param>
        /// <returns>returns true in case of successfull cast</returns>
        public static bool CastStun(
            this Ability ability,
            Unit target,
            Vector3 sourcePosition,
            float straightTimeforSkillShot = 0,
            bool chainStun = true,
            bool useSleep = true,
            string abilityName = null)
        {
            if (!ability.CanBeCasted())
            {
                return false;
            }
            var name = abilityName ?? ability.Name;
            var delay = ability.GetHitDelay(target, name);
            var canUse = Utils.ChainStun(target, delay, null, false, name);
            if (!canUse && chainStun)
            {
                return false;
            }
            if (ability.IsAbilityBehavior(AbilityBehavior.UnitTarget, name) && name != "lion_impale")
            {
                ability.UseAbility(target);
            }
            else if ((ability.IsAbilityBehavior(AbilityBehavior.AreaOfEffect, name)
                      || ability.IsAbilityBehavior(AbilityBehavior.Point, name)))
            {
                if (Prediction.StraightTime(target) > straightTimeforSkillShot * 1000
                    && ability.CastSkillShot(target, name))
                {
                    if (useSleep)
                    {
                        Utils.Sleep(Math.Max(delay, 0.2) * 1000 + 250, "CHAINSTUN_SLEEP");
                    }
                    return true;
                }
                return false;
            }
            else if (ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name))
            {
                if (name == "invoker_ice_wall")
                {
                    ability.CastSkillShot(target, name);
                }
                else
                {
                    ability.UseAbility();
                }
            }
            if (useSleep)
            {
                Utils.Sleep(Math.Max(delay, 0.2) * 1000 + 250, "CHAINSTUN_SLEEP");
            }
            return true;
        }

        /// <summary>
        ///     Returns castpoint of given ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static double FindCastPoint(this Ability ability, string abilityName = null)
        {
            if (ability is Item)
            {
                return 0;
            }
            if (ability.OverrideCastPoint != -1)
            {
                return 0.1;
            }
            var name = abilityName ?? ability.Name;
            double castPoint;
            if (CastPointDictionary.TryGetValue(name + " " + ability.Level, out castPoint))
            {
                return castPoint;
            }
            castPoint = ability.GetCastPoint(ability.Level);
            CastPointDictionary.Add(name + " " + ability.Level, castPoint);
            return castPoint;
        }

        /// <summary>
        ///     Returns ability data with given name, checks if data are level dependent or not
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="dataName"></param>
        /// <param name="level">Custom level</param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static float GetAbilityData(
            this Ability ability,
            string dataName,
            uint level = 0,
            string abilityName = null)
        {
            var lvl = ability.Level;
            var name = abilityName ?? ability.Name;
            AbilityData data;
            if (!DataDictionary.TryGetValue(name + "_" + dataName, out data))
            {
                data = ability.AbilityData.FirstOrDefault(x => x.Name == dataName);
                DataDictionary.Add(name + "_" + dataName, data);
            }
            if (level > 0)
            {
                lvl = level;
            }
            if (data == null)
            {
                return 0;
            }
            return data.Count > 1 ? data.GetValue(lvl - 1) : data.Value;
        }

        /// <summary>
        ///     Returns delay before ability is casted
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="usePing"></param>
        /// <param name="useCastPoint"></param>
        /// <param name="abilityName"></param>
        /// <param name="useChannel"></param>
        /// <returns></returns>
        public static double GetCastDelay(
            this Ability ability,
            Hero source,
            Unit target,
            bool usePing = false,
            bool useCastPoint = true,
            string abilityName = null,
            bool useChannel = false)
        {
            var name = abilityName ?? ability.Name;
            var delay = 0d;
            if (useCastPoint)
            {
                if (!DelayDictionary.TryGetValue(name + " " + ability.Level, out delay))
                {
                    delay = Math.Max(ability.FindCastPoint(name), 0.05);
                    DelayDictionary.Add(name + " " + ability.Level, delay);
                }
                if ((name == "item_diffusal_blade" || name == "item_diffusal_blade_2"))
                {
                    delay += 2;
                }
            }
            else
            {
                if (ability is Item)
                {
                    delay = 0;
                }
                else
                {
                    delay = 0.05;
                }
            }
            if (usePing)
            {
                delay += Game.Ping / 1000;
            }
            if (useChannel)
            {
                delay += ability.ChannelTime(name);
            }
            if (!ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name))
            {
                return
                    Math.Max(
                        delay
                        + (!target.Equals(source)
                               ? (useCastPoint ? source.GetTurnTime(target) : source.GetTurnTime(target) / 2)
                               : 0),
                        0);
            }
            return Math.Max(delay, 0);
        }

        /// <summary>
        ///     Returns cast range of ability, if ability is NonTargeted it will return its radius!
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static float GetCastRange(this Ability ability, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            if (name == "templar_assassin_meld")
            {
                return (ability.Owner as Hero).GetAttackRange() + 50;
            }
            if (!ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name))
            {
                var castRange = ability.CastRange;
                var bonusRange = 0;
                if (castRange <= 0)
                {
                    castRange = 999999;
                }
                if (name == "dragon_knight_dragon_tail"
                    && (ability.Owner as Hero).Modifiers.Any(x => x.Name == "modifier_dragon_knight_dragon_form"))
                {
                    bonusRange = 250;
                }
                else if (name == "beastmaster_primal_roar" && (ability.Owner as Hero).AghanimState())
                {
                    bonusRange = 350;
                }
                return castRange + bonusRange + 100;
            }

            var radius = 0f;
            AbilityInfo data;
            if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
            {
                data = AbilityDatabase.Find(name);
                AbilityDamage.DataDictionary.Add(ability, data);
            }
            if (data == null)
            {
                return ability.CastRange;
            }
            if (!data.FakeCastRange)
            {
                if (data.Width != null)
                {
                    radius = ability.GetAbilityData(data.Width, abilityName: name);
                }
                if (data.StringRadius != null)
                {
                    radius = ability.GetAbilityData(data.StringRadius, abilityName: name);
                }
                if (data.Radius > 0)
                {
                    radius = data.Radius;
                }
            }
            else
            {
                radius = ability.GetAbilityData(data.RealCastRange, abilityName: name);
            }
            return radius;
        }

        /// <summary>
        ///     Checks all aspects and returns full delay before target gets hit by given ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static double GetHitDelay(this Ability ability, Unit target, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            AbilityInfo data;
            if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
            {
                data = AbilityDatabase.Find(name);
                AbilityDamage.DataDictionary.Add(ability, data);
            }
            var owner = ability.Owner as Unit;
            var delay = ability.GetCastDelay(owner as Hero, target, true, abilityName: name);
            if (data != null)
            {
                delay += data.AdditionalDelay;
            }
            var speed = ability.GetProjectileSpeed(name);
            var radius = ability.GetRadius(name);
            if (!ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name) && speed < 6000 && speed > 0)
            {
                var xyz = ability.GetPrediction(target, abilityName: name);
                delay += (Math.Max((owner.Distance2D(xyz) - radius / 2), 100) / speed);
            }
            return delay;
        }

        /// <summary>
        ///     Returns prediction for given target after given ability hit delay
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="target"></param>
        /// <param name="customDelay">enter your custom delay</param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static Vector3 GetPrediction(
            this Ability ability,
            Unit target,
            double customDelay = 0,
            string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            AbilityInfo data;
            if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
            {
                data = AbilityDatabase.Find(name);
                AbilityDamage.DataDictionary.Add(ability, data);
            }
            var owner = ability.Owner as Unit;
            var delay = ability.GetCastDelay(owner as Hero, target, true, abilityName: name);
            if (data != null)
            {
                delay += data.AdditionalDelay;
            }
            var speed = ability.GetProjectileSpeed(name);
            var radius = ability.GetRadius(name);
            Vector3 xyz;
            if (speed > 0 && speed < 6000)
            {
                xyz = Prediction.SkillShotXYZ(
                    owner,
                    target,
                    (float)((delay + owner.GetTurnTime(target.Position)) * 1000),
                    speed,
                    radius);
                if (!ability.IsAbilityBehavior(AbilityBehavior.NoTarget, name))
                {
                    xyz = Prediction.SkillShotXYZ(
                        owner,
                        target,
                        (float)((delay + (float)owner.GetTurnTime(xyz)) * 1000),
                        speed,
                        radius);
                }
            }
            else
            {
                xyz = Prediction.PredictedXYZ(target, (float)(delay * 1000));
            }
            return xyz;
        }

        /// <summary>
        ///     Returns projectile speed of the ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static float GetProjectileSpeed(this Ability ability, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            float speed;
            if (!SpeedDictionary.TryGetValue(name + " " + ability.Level, out speed))
            {
                AbilityInfo data;
                if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
                {
                    data = AbilityDatabase.Find(name);
                    AbilityDamage.DataDictionary.Add(ability, data);
                }
                if (data == null)
                {
                    speed = float.MaxValue;
                    SpeedDictionary.Add(name + " " + ability.Level, speed);
                    return speed;
                }
                if (data.Speed != null)
                {
                    speed = ability.GetAbilityData(data.Speed, abilityName: name);
                    SpeedDictionary.Add(name + " " + ability.Level, speed);
                }
            }
            return speed;
        }

        /// <summary>
        ///     Returns impact radius of given ability
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static float GetRadius(this Ability ability, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            float radius;
            if (!RadiusDictionary.TryGetValue(name + " " + ability.Level, out radius))
            {
                AbilityInfo data;
                if (!AbilityDamage.DataDictionary.TryGetValue(ability, out data))
                {
                    data = AbilityDatabase.Find(name);
                    AbilityDamage.DataDictionary.Add(ability, data);
                }
                if (data == null)
                {
                    radius = 0;
                    RadiusDictionary.Add(name + " " + ability.Level, radius);
                    return radius;
                }
                if (data.Width != null)
                {
                    radius = ability.GetAbilityData(data.Width, abilityName: name);
                    RadiusDictionary.Add(name + " " + ability.Level, radius);
                    return radius;
                }
                if (data.StringRadius != null)
                {
                    radius = ability.GetAbilityData(data.StringRadius, abilityName: name);
                    RadiusDictionary.Add(name + " " + ability.Level, radius);
                    return radius;
                }
                if (data.Radius > 0)
                {
                    radius = data.Radius;
                    RadiusDictionary.Add(name + " " + ability.Level, radius);
                    return radius;
                }
                if (data.IsBuff)
                {
                    radius = (ability.Owner as Hero).GetAttackRange();
                    RadiusDictionary.Add(name + " " + ability.Level, radius);
                    return radius;
                }
            }
            return radius;
        }

        /// <summary>
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static float ChannelTime(this Ability ability, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            float channel;
            if (!ChannelDictionary.TryGetValue(name + ability.Level, out channel))
            {
                channel = ability.GetChannelTime(ability.Level - 1);
                ChannelDictionary.Add(name + ability.Level, channel);
            }
            //Console.WriteLine(ability.GetChannelTime(ability.Level - 1) + "  " + delay + " " + name);
            return channel;
        }

        /// <summary>
        ///     Checks if this ability can be casted by Invoker, if the ability is not currently invoked, it is gonna check for
        ///     both invoke and the ability manacost.
        /// </summary>
        /// <param name="ability">given ability</param>
        /// <param name="invoke">invoker ultimate</param>
        /// <param name="spell4">current spell on slot 4</param>
        /// <param name="spell5">current spell on slot 5</param>
        /// <returns></returns>
        public static bool InvoCanBeCasted(this Ability ability, Ability invoke, Ability spell4, Ability spell5)
        {
            var owner = ability.Owner as Hero;
            if (owner == null)
            {
                return false;
            }
            if (!(ability is Item) && ability.Name != "invoker_invoke" && ability.Name != "invoker_quas"
                && ability.Name != "invoker_wex" && ability.Name != "invoker_exort" && !ability.Equals(spell4)
                && !ability.Equals(spell5))
            {
                return invoke.Level > 0 && invoke.Cooldown <= 0 && ability.Cooldown <= 0
                       && (ability.ManaCost + invoke.ManaCost) <= owner.Mana;
            }
            return ability.AbilityState == AbilityState.Ready;
        }

        /// <summary>
        ///     Checks if given ability has given ability behaviour flag
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="flag"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool IsAbilityBehavior(this Ability ability, AbilityBehavior flag, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            AbilityBehavior data;
            if (AbilityBehaviorDictionary.TryGetValue(name, out data))
            {
                return data.HasFlag(flag);
            }
            data = ability.AbilityBehavior;
            AbilityBehaviorDictionary.Add(name, data);
            return data.HasFlag(flag);
        }

        /// <summary>
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilityName"></param>
        /// <returns></returns>
        public static bool RequiresCharges(this Ability ability, string abilityName = null)
        {
            var name = abilityName ?? ability.Name;
            try
            {
                return Game.FindKeyValues(name + "/ItemRequiresCharges", KeyValueSource.Ability).IntValue == 1;
            }
            catch (KeyValuesNotFoundException)
            {
                return false;
            }
        }

        #endregion
    }
}