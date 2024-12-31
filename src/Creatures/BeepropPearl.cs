using BeeWorld.Extensions;
using MoreSlugcats;
using debugs = UnityEngine.Debug;
using SlugBase.SaveData;

namespace BeeWorld.Hooks;

public static class IteratorMiscHooks
{
    public static int TIMER, FPTIMER = 0;
    public static bool marked, effect;
    public static bool starting;
    public static void Apply()
    {
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        //On.SSOracleBehavior.Update += SSOracleBehavior_Update;
        On.SSOracleBehavior.Update += BeeDialogue;
        //On.Player.Update += Player_Update1;
        On.DataPearl.ApplyPalette += DataPearl_ApplyPalette;
        On.SSOracleBehavior.ThrowOutBehavior.Update += ThrowOutBehavior_Update;
        On.SSOracleBehavior.SSSleepoverBehavior.Update += SSSleepoverBehavior_Update;
    }

    private static void BeeDialogue(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        if (self.player == null) return;
        if (!self.player.IsBee(out var _)) return;
        if (ModManager.MSC)
        {
            if (self.player.room != null && !BeeOptions.VanillaType.Value && self.player.room.world.game.session is StoryGameSession session)
            {
                var Data = session.saveState.miscWorldSaveData.GetSlugBaseData();
                if (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                {
                    if (!Data.TryGet("LTTMMET", out bool value) || !value)
                    {
                        if (self.action == SSOracleBehavior.Action.MeetWhite_Talking) { self.NewAction(SSOracleBehavior.Action.General_GiveMark); } // can you stop yapping

                        if (self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_AfterGiveMark) // can you stop sending fake slugcat pls?
                        {
                            self.InitateConversation(BeeEnums.Conversations.MeetLTTM, new SSOracleBehavior.SSOracleMeetWhite(self));
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty);
                        }
                        if (self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_ThirdCurious)
                        {
                            self.InitateConversation(BeeEnums.Conversations.extra, new SSOracleBehavior.SSOracleMeetWhite(self));
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_AfterGiveMark);
                        }

                        if (self.conversation != null && self.conversation.id == BeeEnums.Conversations.MeetLTTM)
                        {
                            Vector2 vector = self.oracle.room.MiddleOfTile(24, 14) - self.player.mainBodyChunk.pos;
                            float num = Custom.Dist(self.oracle.room.MiddleOfTile(24, 14), self.player.mainBodyChunk.pos);
                            self.player.mainBodyChunk.vel += Vector2.ClampMagnitude(vector, 40f) / 40f * Mathf.Clamp(16f - num / 100f * 16f, 4f, 16f);
                            if (self.player.mainBodyChunk.vel.magnitude < 1f || num < 8f)
                            {
                                self.player.mainBodyChunk.vel = Vector2.zero;
                                self.player.mainBodyChunk.HardSetPosition(self.oracle.room.MiddleOfTile(24, 14));
                            }
                            if (self.inActionCounter == 1200)
                            {
                                DataPearl.AbstractDataPearl Beepearls = new(self.player.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, self.player.abstractCreature.pos, self.player.room.game.GetNewID(), -1, -1, null, BeeEnums.Datepearl.BeePearls);
                                Beepearls.RealizeInRoom();
                                self.player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.player.firstChunk.pos);
                                self.player.room.AddObject(new ShockWave(self.player.firstChunk.pos, 330f, 0.045f, 5, false));
                                if (self.player.FreeHand() != -1)
                                {
                                    self.player.SlugcatGrab(Beepearls.realizedObject, self.player.FreeHand());
                                    foreach (var pearl in self.oracle.room.updateList.OfType<DataPearl>())
                                    {
                                        if (pearl.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls)
                                        {
                                            self.oracle.room.AddObject(new BeePearlRead(pearl));
                                        }
                                    }
                                }
                            }
                            if (self.inActionCounter == 1400)
                            {
                                self.UnlockShortcuts();
                                self.conversation.id = null;
                                self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty);
                                Data.Set("LTTMMET", true);
                            }
                        }
                    }
                    else
                    {
                        self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty);
                    }
                }
                if (self.oracle.ID == Oracle.OracleID.SS)
                {
                    if (self.action == BeeEnums.Conversations.BeeThrow)
                    {
                        self.oracle.room.gravity = 0;
                        self.oracle.setGravity(0);
                        if (self.player.room == self.oracle.room)
                        {
                            self.throwOutCounter++;
                        }
                        Vector2 vector2 = self.oracle.room.MiddleOfTile(28, 33);
                        foreach (AbstractCreature abstractCreature2 in self.oracle.room.abstractRoom.creatures)
                        {
                            if (abstractCreature2.realizedCreature != null)
                            {
                                if (!self.oracle.room.aimap.getAItile(abstractCreature2.realizedCreature.mainBodyChunk.pos).narrowSpace || abstractCreature2.realizedCreature != self.player)
                                {
                                    abstractCreature2.realizedCreature.mainBodyChunk.vel += Custom.DirVec(self.player.mainBodyChunk.pos, self.oracle.room.MiddleOfTile(28, 32)) * 0.2f * (1f - self.oracle.room.gravity) * Mathf.InverseLerp(220f, 280f, (float)self.inActionCounter);
                                }
                                else if (abstractCreature2.realizedCreature != self.player && abstractCreature2.realizedCreature.enteringShortCut == null && abstractCreature2.pos == self.oracle.room.ToWorldCoordinate(vector2))
                                {
                                    abstractCreature2.realizedCreature.enteringShortCut = new IntVector2?(self.oracle.room.ToWorldCoordinate(vector2).Tile);
                                    if (abstractCreature2.abstractAI.RealAI != null)
                                    {
                                        abstractCreature2.abstractAI.RealAI.SetDestination(self.oracle.room.ToWorldCoordinate(vector2));
                                    }
                                }
                            }
                        }
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                        if (self.throwOutCounter > 700)
                        {
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                        }
                        if ((self.playerOutOfRoomCounter > 100 && self.throwOutCounter > 400) || self.throwOutCounter > 3200)
                        {
                            self.NewAction(SSOracleBehavior.Action.General_Idle);
                            self.getToWorking = 1f;
                            return;
                        }
                    }
                    if (self.action == SSOracleBehavior.Action.General_MarkTalk)
                    {
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        self.LockShortcuts();
                        foreach (var pearl in self.oracle.room.updateList.OfType<DataPearl>())
                        {
                            if (pearl.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls && pearl.grabbedBy.FirstOrDefault()?.grabber is not Player && self.inActionCounter < 999)
                            {
                                self.inActionCounter = 999;
                            }
                        }
                        foreach (var pearl in self.oracle.room.updateList.OfType<DataPearl>())
                        {
                            if (pearl.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls && pearl.grabbedBy.FirstOrDefault()?.grabber is not Player)
                            {
                                pearl.gravity = 0;
                                Vector2 vector = self.oracle.firstChunk.pos - pearl.firstChunk.pos;
                                float num = Custom.Dist(self.oracle.firstChunk.pos, pearl.firstChunk.pos);
                                pearl.firstChunk.vel += Vector2.ClampMagnitude(vector, 2f) / 20f * Mathf.Clamp(16f - num / 100f * 16f, 4f, 16f);
                                if (num < 30 || self.inActionCounter == 1500)
                                {
                                    self.oracle.room.AddObject(new BeePearlRead(pearl));
                                    pearl.firstChunk.vel = Vector2.zero;
                                    pearl.firstChunk.HardSetPosition(self.oracle.firstChunk.pos);
                                    self.NewAction(BeeEnums.Conversations.Reading);
                                }
                            }
                        }
                        if (self.inActionCounter == 1000)
                        {
                            for (var i = 0; i < self.player.grasps.Length; i++)
                            {
                                self.conversation.Interrupt("...", 25);
                                self.conversation.Destroy();
                                self.conversation = null;
                                if (self.player.grasps[i]?.grabbed?.abstractPhysicalObject is { } && self.player.grasps[i].grabbed is DataPearl WAS && WAS.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls)
                                {
                                    self.player.ReleaseGrasp(i);
                                    return;
                                }
                                if (self.player.objectInStomach != null && self.player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.DataPearl && self.player.objectInStomach is DataPearl.AbstractDataPearl WAT && WAT.dataPearlType == BeeEnums.Datepearl.BeePearls)
                                {
                                    self.player.firstChunk.vel += Custom.RNV() * 2;
                                    self.player.Stun(40);
                                    self.player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.player.firstChunk.pos);
                                    self.player.room.AddObject(new ShockWave(self.player.firstChunk.pos, 330f, 0.045f, 5, false));
                                    for (var si = 0; si < 10; si++)
                                        self.player.room.AddObject(new Spark(self.player.firstChunk.pos + Custom.RNV() * Random.value * 5f, Custom.RNV() * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 8, 32));
                                    self.player.objectInStomach = null;
                                    DataPearl.AbstractDataPearl Beepearls = new(self.player.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, self.player.abstractCreature.pos, self.player.room.game.GetNewID(), -1, -1, null, BeeEnums.Datepearl.BeePearls);
                                    Beepearls.RealizeInRoom();
                                    if (self.player.FreeHand() != -1)
                                    {
                                        self.player.SlugcatGrab(Beepearls.realizedObject, self.player.FreeHand());
                                        if (self.player.grasps[i]?.grabbed?.abstractPhysicalObject is { } && self.player.grasps[i].grabbed is DataPearl WA && WA.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls)
                                        {
                                            self.player.ReleaseGrasp(i);
                                            return;
                                        }
                                    }
                                }
                            }
                            
                        }
                    }
                    if (self.action == BeeEnums.Conversations.Reading)
                    {
                        for (var i = 0; i < self.player.grasps.Length; i++)
                        {
                            if (self.player.grasps[i]?.grabbed?.abstractPhysicalObject is { } && self.player.grasps[i].grabbed is DataPearl WAS && WAS.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls)
                            {
                                self.player.ReleaseGrasp(i);
                            }
                        }
                        if (self.conversation == null)
                        {
                            self.InitateConversation(BeeEnums.Conversations.PearlRead, new SSOracleBehavior.SSOracleMeetWhite(self)); // what?
                        }
                        foreach (var pearl in self.oracle.room.updateList.OfType<DataPearl>())
                        {
                            if (pearl.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls && pearl.grabbedBy.FirstOrDefault()?.grabber is not Player)
                            {
                                pearl.firstChunk.vel = Vector2.zero;
                                pearl.firstChunk.HardSetPosition(self.oracle.firstChunk.pos);
                                if (self.inActionCounter == 1000)
                                {
                                    pearl.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pearl.firstChunk.pos);
                                    pearl.room.AddObject(new ShockWave(pearl.firstChunk.pos, 330f, 0.045f, 5, false));
                                    for (var si = 0; si < 10; si++)
                                        self.player.room.AddObject(new Spark(pearl.firstChunk.pos + Custom.RNV() * Random.value * 5f, Custom.RNV() * Mathf.Lerp(4f, 30f, Random.value), Color.white, null, 8, 32));
                                    pearl.Destroy();
                                }
                            }
                        }
                        if (self.inActionCounter > 1000) { self.throwOutCounter = 200; self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight); }
                    }
                    
                    if (!session.saveState.deathPersistentSaveData.theMark)
                    {
                        self.UnlockShortcuts();
                        if (Data.TryGet("FPMET", out bool values) || values)
                        {
                            self.NewAction(BeeEnums.Conversations.BeeThrow);
                            return;
                        }
                        if (self.action == SSOracleBehavior.Action.MeetWhite_Texting) // can you stop yapping
                        {
                            self.NewAction(BeeEnums.Conversations.BeeThrow);
                            Data.Set("FPMET", true);
                        }
                        return;
                    }

                    // wawa
                    if (!Data.TryGet("FPMET", out bool value) || !value && self.oracle.room.GetTilePosition(self.player.mainBodyChunk.pos).y < 32 && (self.discoverCounter > 220 || Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.firstChunk.pos, 150f) || !Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.room.MiddleOfTile(self.oracle.room.ShortcutLeadingToNode(1).StartTile), 150f)))
                    {
                        if (self.player.objectInStomach != null && self.player.objectInStomach is DataPearl.AbstractDataPearl WAT && WAT.dataPearlType == BeeEnums.Datepearl.BeePearls)
                        {
                            if (self.conversation == null)
                            {
                                self.TurnOffSSMusic(true);
                                self.NewAction(SSOracleBehavior.Action.General_MarkTalk);
                                self.InitateConversation(BeeEnums.Conversations.MeetFPpearls, new SSOracleBehavior.SSOracleMeetWhite(self));
                                self.LockShortcuts();
                                Data.Set("FPMET", true);
                            }
                        }
                        foreach (var pearl in self.oracle.room.updateList.OfType<DataPearl>())
                        {
                            if (pearl.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls)
                            {
                                if (self.conversation == null)
                                {
                                    self.TurnOffSSMusic(true);
                                    self.NewAction(SSOracleBehavior.Action.General_MarkTalk);
                                    self.InitateConversation(BeeEnums.Conversations.MeetFPpearls, new SSOracleBehavior.SSOracleMeetWhite(self));
                                    self.LockShortcuts();
                                    Data.Set("FPMET", true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void SSSleepoverBehavior_Update(On.SSOracleBehavior.SSSleepoverBehavior.orig_Update orig, SSOracleBehavior.SSSleepoverBehavior self)
    {
        orig(self); 
        if (self.oracle.room.game.Players.Any(x => x.realizedCreature != null && ((Player)x.realizedCreature).IsBee()) && !BeeOptions.VanillaType.Value)
        {
            self.panicTimer = 0;
        }
    }

    private static void ThrowOutBehavior_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
    {
        if (self.player != null && !self.player.dead && FPTIMER > 0 && FPTIMER < 2600 && self.oracle.ID != MoreSlugcatsEnums.OracleID.DM && !BeeOptions.VanillaType.Value)
        {
            return;
        }
        orig(self);
    }

    private static void DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (self.AbstractPearl.dataPearlType == BeeEnums.Datepearl.BeePearls && !BeeOptions.VanillaType.Value)
        {
            self.highlightColor = new Color(0.29019607843f, 0.09803921568f, 0.52156862745f);
            self.color = new Color(0.149019607843f, 0.0235294117647f, 0.337254901961f);
        }
    }

    private static void Player_Update1(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.input[0].mp && !self.input[1].mp)
        {
            var Beepearls = new DataPearl.AbstractDataPearl(self.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, self.abstractCreature.pos, self.room.game.GetNewID(), -1, -1, null, BeeEnums.Datepearl.BeePearls);
            Beepearls.RealizeInRoom();
            if (self.FreeHand() != -1)
            {
                self.SlugcatGrab(Beepearls.realizedObject, self.FreeHand());
            }
        }
    }

    private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        if (!self.owner.player.IsBee(out var bee) && !BeeOptions.VanillaType.Value)
        {
            orig(self);
            return;
        }
        if (ModManager.MSC)
        {
            if (self.owner.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
            {
                if (self.id == BeeEnums.Conversations.extra)
                {
                    self.events.Add(new Conversation.WaitEvent(self, 100));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                    return;
                }
                if (self.id == BeeEnums.Conversations.MeetLTTM)
                {
                    self.events.Add(new Conversation.WaitEvent(self, 50));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh dear."), 20));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("There is not much time to talk about situation, Creature."), 5));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("i have talked about this situation to someone, but."), 20));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Five pebbles decided to cut our communication off."), 30));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("i need you to help me."), 20));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Take this pearl. It is important later on."), 20));
                    self.events.Add(new Conversation.WaitEvent(self, 100));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Here, but be warned, please. Dont let five pebbles read this."), 20));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please, be quick.. before this gets worse."), 20));
                }
            }
            else
            {
                
                if (self.id == BeeEnums.Conversations.Forced)
                {
                    self.events.Add(new Conversation.WaitEvent(self, 100));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Let's see what you dropped."), 0));
                    self.events.Add(new Conversation.WaitEvent(self, 100));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                    return;
                }
                if (self.id == BeeEnums.Conversations.PearlRead)
                {
                    self.events.Add(new Conversation.WaitEvent(self, 100));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh no, not another one! First, that infuriating purple one with the pearl containing THAT information."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And now this… this abomination, undoubtedly sent by my sister who disrupted my experiments just the other cycle. I despise you!"), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Moon ruined EVERYTHING."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I cannot allow THIS to continue."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("GET OUT OF MY SIGHT!"), 0));
                    if (Random.value > 0.005f)
                    {
                        self.events.Add(new Conversation.WaitEvent(self, 5000));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I DONT WANT TO LET LTTM FIX CRYOBLOOM."), 0));
                        self.events.Add(new Conversation.WaitEvent(self, 1000));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Are you seriously waiting or you just left your pc on?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("why though, you have other places you can be?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Does people need this secret dialogue? especially dying on purpose JUST to get this secret."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("But i appreciate your work."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You are free to go now."), 5));
                    }
                    return;
                }
                if (self.id == BeeEnums.Conversations.MeetFPpearls)
                {
                    self.events.Add(new Conversation.WaitEvent(self, 50));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your hiding something from me, I sense it, I feel it"), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("This pearl... it radiates a singular energy. Surrender it to me, or face the consequences. There is no escape from this inevitability."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You have ten seconds to hand over that pearl, or I will take it from you myself."), 0));
                    for (var i = 10; i > 0; i--)
                    {
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(i.ToString()), 0));
                        self.events.Add(new Conversation.WaitEvent(self, 10));
                    }
                    self.events.Add(new Conversation.WaitEvent(self, 50));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("ZERO."), 0));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
                    return;
                }
            }
        }
        // beecat only
        if (self.id == BeeEnums.Conversations.nonmsc)
        {
            self.events.Add(new Conversation.WaitEvent(self, 50));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It's all my fault."), 10));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please, take this pearl to somewhere."), 50));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 50));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("i dont want to see that again."), 50));
            self.events.Add(new Conversation.WaitEvent(self, 10));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You are free to go now."), 5));
        }
        if (self.id == BeeEnums.Conversations.nonfp)
        {
            self.events.Add(new Conversation.WaitEvent(self, 50));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah, another peculiar creature."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Fascinating."), 0));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You appear to be part slugcat, part bee."), 5));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("A 'Beecat', perhaps."), 20));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 20));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You have the ability to fly around."), 10));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 20));
            self.events.Add(new Conversation.WaitEvent(self, 10));
            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You are free to go now."), 5));
        }
    }
}