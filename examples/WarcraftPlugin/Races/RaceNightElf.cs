/*
 *  This file is part of CounterStrikeSharp.
 *  CounterStrikeSharp is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  CounterStrikeSharp is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with CounterStrikeSharp.  If not, see <https://www.gnu.org/licenses/>. *
 */

using System;
using System.Drawing;
using WarcraftPlugin.Effects;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Numerics;

namespace WarcraftPlugin.Races
{
    public class RaceNightElf : WarcraftRace
    {
        public override string InternalName => "night_elf";
        public override string DisplayName => "Night Elf";

        private float[]EvasionChance = {0.0f,0.05f,0.1f,0.15f,0.2f};
        private float[]ThornsAura = {0.0f,0.05f,0.1f,0.15f,0.2f};
        private float[]TrueShotAura = {0.0f,0.05F,0.1f,0.15f,0.2f};
        private float[]RootsTime = {0.0f,1.25f,1.5f,1.75f,2.0f};
        public override void Register()
        {
            AddAbility(new SimpleWarcraftAbility("evasion", "Evasion",
                i => $" {ChatColors.Green}5/10/15/20{ChatColors.Default} percent chance of evading a shot"));
            AddAbility(new SimpleWarcraftAbility("thornsaura", "Thorns Aura",
                i => $"You deal {ChatColors.Green}5/10/15/20{ChatColors.Default} percent of damage recieved to your attacker"));
            AddAbility(new SimpleWarcraftAbility("trueshotaura", "Trueshot Aura",
                i =>
                    $"Your attacks deal{ChatColors.Green} 5/10/15/20 {ChatColors.Default}percent more damage"));
            AddAbility(new SimpleCooldownAbility("entanglingroots", "Entangling Roots",
                i =>
                    $"Bind enemies to the ground, rendering them immobile for{ChatColors.Green} 1.25/1.5/1.75/2{ChatColors.Default} seconds",
                20.0f));

            HookEvent<EventPlayerHurt>("player_hurt_pre", PlayerHurtPre);
            HookEvent<EventPlayerHurt>("player_hurt_pre_othrer", PlayerHurtPreOther);
            HookAbility(3, Ultimate);
        }

        private void Ultimate()
        {
            if (WarcraftPlayer.GetAbilityLevel(3) < 1) return;

            if (IsAbilityReady(3))
            {
                EntanglingRoots(600f);
                StartCooldown(3);
            }
        }

        private void EntanglingRoots(float distance)
        {
            var AttackerOrigin = Player.AbsOrigin;
            var playerEntities = CounterStrikeSharp.API.Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
            foreach (var victim in playerEntities)
            {
                if (!victim.IsValid || !victim.PawnIsAlive) continue;
                if(victim.TeamNum == Player.TeamNum) continue;
                var VictimOrigin = victim.AbsOrigin;
                if(Vector3.Distance(new Vector3(AttackerOrigin.X,AttackerOrigin.Y,AttackerOrigin.Z),new Vector3(VictimOrigin.X,VictimOrigin.Y,VictimOrigin.Z))<= 1200.0f)
                {
                    DispatchEffect(new RootsEffect(Player, victim, RootsTime[WarcraftPlayer.GetAbilityLevel(3)]));
                    Player.PrintToChat($"{ChatColors.Green}{victim.PlayerName} {ChatColors.Gold} entangled in roots");
                }
            }

        }
        
        private void PlayerHurtPre(GameEvent @obj)
        {
            var @event = (EventPlayerHurt)obj;
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if(victim != attacker)
            {
                victim.PrintToChat("Виктим=! атакер");
                var chance = new Random().Next(0,100)*1.0;
                int EvasionLevel = WarcraftPlayer.GetAbilityLevel(0);
                victim.PrintToChat($"Уровень скилла {EvasionLevel.ToString()}");
                bool ev = false;
                victim.PrintToChat($" шансы");
                victim.PrintToChat($"шанс {chance.ToString()} ");
                float ec = EvasionChance[EvasionLevel];

                if(chance<=50)
                {
                    @event.DmgHealth = 0;
                    victim.PrintToChat($"{ChatColors.Gold} You dodged a shot");
                    ev = true;
                }


                if(!ev)
                {
                    int ThornsAuraLevel = WarcraftPlayer.GetAbilityLevel(1);
                    var damagepercent = ThornsAura[ThornsAuraLevel];
                    var dealdamage = @event.DmgHealth*damagepercent;
                    //тут надо дописать урон, а его нету
                }
            }
        }
        private void PlayerHurtPreOther(GameEvent @obj)
        {
            var @event = (EventPlayerHurt)obj;
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if(victim != attacker)
            {
                int TrueShotLevel = WarcraftPlayer.GetAbilityLevel(2);
                var TrueShotPercent = TrueShotAura[TrueShotLevel];
                if(TrueShotLevel>0) @event.DmgHealth = Convert.ToInt32(@event.DmgHealth*TrueShotPercent);
            }
        }

        public override void PlayerChangingToAnotherRace()
        {
            base.PlayerChangingToAnotherRace();
        }
    }
    
    public class RootsEffect : WarcraftEffect
    {
        public RootsEffect(CCSPlayerController owner, CCSPlayerController target, float duration) : base(owner, target,
            duration)
        {
        }

        public override void OnStart()
        {
            Target.GetWarcraftPlayer()?.SetStatusMessage($"{ChatColors.Green}[Roots]{ChatColors.Default}", Duration);
            Target.PlayerPawn.Value.AbsVelocity.X = 0.0f;
            Target.PlayerPawn.Value.AbsVelocity.Y = 0.0f;
            Target.PlayerPawn.Value.AbsVelocity.Z = 0.0f;
            Target.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NONE;
            
            // Target.RenderMode = RenderMode.RENDER_TRANSCOLOR;
            // Target.Color = Color.FromArgb(255, 50, 50, 255);
            Console.WriteLine("Added roots");
        }

        public override void OnTick()
        {
            base.OnTick();
            Console.WriteLine("Roots tick");
        }

        public override void OnFinish()
        {
            Target.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
            // Target.RenderMode = RenderMode.RENDER_NORMAL;
            // Target.Color = Color.FromArgb(255, 255, 255, 255);
            Console.WriteLine("Roots finished");
        }
    }
}