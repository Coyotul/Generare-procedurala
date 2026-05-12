using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Lab6
{
    public class Lab6Main : MonoBehaviour
    {
        public enum Tab { Npc, Item, World, Gallery }

        private SaveData _save;
        private Lab6World _world;

        private Canvas _canvas;
        private RectTransform _content;
        private readonly Dictionary<Tab, GameObject> _panels = new Dictionary<Tab, GameObject>();
        private Tab _currentTab = Tab.Npc;

        // NPC tab
        private Npc _currentNpc;
        private RectTransform _npcDisplay;

        // Item tab
        private Item _currentItem;
        private RectTransform _itemDisplay;
        private Text _itemNameText;

        // World tab
        private Text _worldStatusText;
        private RectTransform _journalContent;
        private RectTransform _mapRoot;

        // Gallery tab
        private RectTransform _galleryContent;
        private bool _galleryShowItems = false;

        private void Start()
        {
            EnsureEventSystem();
            BuildCanvas();
            _save = Lab6SaveSystem.Load();
            BuildHeader();
            BuildPanels();
            ShowTab(Tab.Npc);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("Lab6Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasGo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280, 720);
            cs.matchWidthOrHeight = 0.5f;

            GameObject bg = Lab6Ui.Panel(_canvas.transform, "Background", new Color(0.12f, 0.13f, 0.17f));
            Lab6Ui.Stretch(bg);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(_canvas.transform, false);
            _content = (RectTransform)content.transform;
            Lab6Ui.Stretch(content, 12);
        }

        private void BuildHeader()
        {
            GameObject header = Lab6Ui.Panel(_content, "Header", new Color(0.16f, 0.18f, 0.24f));
            RectTransform rt = (RectTransform)header.transform;
            Lab6Ui.Anchor(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -48), new Vector2(0, 0));

            HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            Lab6Ui.Label(header.transform, "<b>Lab 6 - NPC / Item / World</b>", 18, TextAnchor.MiddleLeft, FontStyle.Bold);

            foreach (Tab t in new[] { Tab.Npc, Tab.Item, Tab.World, Tab.Gallery })
            {
                Tab captured = t;
                Lab6Ui.Btn(header.transform, t.ToString(), () => ShowTab(captured));
            }
        }

        private void BuildPanels()
        {
            _panels[Tab.Npc]     = BuildNpcPanel();
            _panels[Tab.Item]    = BuildItemPanel();
            _panels[Tab.World]   = BuildWorldPanel();
            _panels[Tab.Gallery] = BuildGalleryPanel();
        }

        private RectTransform MakePanelArea(string name)
        {
            GameObject p = Lab6Ui.Panel(_content, name, new Color(0.10f, 0.11f, 0.15f));
            RectTransform rt = (RectTransform)p.transform;
            Lab6Ui.Anchor(rt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -56));
            return rt;
        }

        private void ShowTab(Tab tab)
        {
            _currentTab = tab;
            foreach (var kv in _panels) kv.Value.SetActive(kv.Key == tab);
            if (tab == Tab.Gallery) RefreshGallery();
            if (tab == Tab.World) RefreshWorldUi();
        }

        // ====================== NPC TAB ======================
        private GameObject BuildNpcPanel()
        {
            RectTransform area = MakePanelArea("NpcPanel");

            // Top action bar
            GameObject actions = Lab6Ui.Panel(area, "Actions", new Color(0.14f, 0.16f, 0.22f));
            RectTransform actRt = (RectTransform)actions.transform;
            Lab6Ui.Anchor(actRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -52), new Vector2(-8, -8));
            HorizontalLayoutGroup hlg = actions.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            Button gen = Lab6Ui.Btn(actions.transform, "Generate NPC", GenerateNpcAndSave, new Color(0.35f, 0.55f, 0.30f));
            LayoutElement le = gen.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 180;
            le.minWidth = 180;

            Lab6Ui.Label(actions.transform, "Weighted classes: Warrior 30 / Mage 20 / Rogue 25 / Archer 15 / Paladin 10", 12, TextAnchor.MiddleLeft);

            // Display area
            GameObject display = Lab6Ui.Panel(area, "Display", new Color(0.13f, 0.14f, 0.20f));
            RectTransform dRt = (RectTransform)display.transform;
            Lab6Ui.Anchor(dRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -60));
            _npcDisplay = dRt;
            return area.gameObject;
        }

        private void GenerateNpcAndSave()
        {
            Npc n = Lab6NpcGenerator.Generate(_save.nextNpcId++);
            _save.npcs.Add(n);
            Lab6SaveSystem.Save(_save);
            _currentNpc = n;
            RenderNpcDisplay();
        }

        private void RenderNpcDisplay()
        {
            ClearChildren(_npcDisplay);
            if (_currentNpc == null)
            {
                Lab6Ui.Label(_npcDisplay, "Press <b>Generate NPC</b> to roll a new character.", 14, TextAnchor.MiddleCenter);
                return;
            }

            // Portrait (left)
            GameObject portrait = Lab6Ui.Panel(_npcDisplay, "Portrait", Lab6Colors.ForClass(_currentNpc.cls));
            RectTransform pRt = (RectTransform)portrait.transform;
            Lab6Ui.Anchor(pRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 16), new Vector2(0, -16));
            pRt.sizeDelta = new Vector2(220, 0);

            Text initial = Lab6Ui.Label(portrait.transform,
                _currentNpc.cls.ToString().Substring(0, 1),
                160, TextAnchor.MiddleCenter, FontStyle.Bold);
            initial.color = new Color(1, 1, 1, 0.85f);
            Lab6Ui.Stretch(initial.gameObject);

            Text clsLabel = Lab6Ui.Label(portrait.transform, _currentNpc.cls.ToString(), 16, TextAnchor.LowerCenter, FontStyle.Bold);
            RectTransform clRt = clsLabel.GetComponent<RectTransform>();
            Lab6Ui.Anchor(clRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 6), new Vector2(0, 28));

            // Stats panel (right)
            GameObject stats = Lab6Ui.Panel(_npcDisplay, "Stats", new Color(0.10f, 0.11f, 0.16f));
            RectTransform sRt = (RectTransform)stats.transform;
            Lab6Ui.Anchor(sRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(252, 16), new Vector2(-16, -16));
            VerticalLayoutGroup vlg = stats.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 6;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;

            Lab6Ui.Label(stats.transform, $"<b>{_currentNpc.name}</b>", 24, TextAnchor.UpperLeft, FontStyle.Bold);
            Lab6Ui.Label(stats.transform, $"<i>{_currentNpc.cls}</i>", 16, TextAnchor.UpperLeft, FontStyle.Italic);
            Lab6Ui.Label(stats.transform, "", 6);
            Lab6Ui.Label(stats.transform, $"HP:     {Mathf.RoundToInt(_currentNpc.hp)} / {Mathf.RoundToInt(_currentNpc.maxHp)}", 16);
            Lab6Ui.Label(stats.transform, $"Damage: {Mathf.RoundToInt(_currentNpc.damage)}", 16);
            Lab6Ui.Label(stats.transform, $"Armor:  {Mathf.RoundToInt(_currentNpc.armor)}", 16);
            Lab6Ui.Label(stats.transform, "", 6);
            Lab6Ui.Label(stats.transform, $"<b>Trait:</b> {Lab6NpcGenerator.TraitDescription(_currentNpc.trait)}", 14);
        }

        // ====================== ITEM TAB ======================
        private GameObject BuildItemPanel()
        {
            RectTransform area = MakePanelArea("ItemPanel");

            GameObject actions = Lab6Ui.Panel(area, "Actions", new Color(0.14f, 0.16f, 0.22f));
            RectTransform actRt = (RectTransform)actions.transform;
            Lab6Ui.Anchor(actRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -52), new Vector2(-8, -8));
            HorizontalLayoutGroup hlg = actions.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            Button gen = Lab6Ui.Btn(actions.transform, "Generate Item", GenerateItemAndSave, new Color(0.40f, 0.40f, 0.65f));
            LayoutElement le = gen.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 180; le.minWidth = 180;

            Lab6Ui.Label(actions.transform, "Common 55 / Uncommon 25 / Rare 12 / Epic 6 / Legendary 2  (%)", 12, TextAnchor.MiddleLeft);

            GameObject display = Lab6Ui.Panel(area, "Display", new Color(0.13f, 0.14f, 0.20f));
            RectTransform dRt = (RectTransform)display.transform;
            Lab6Ui.Anchor(dRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -60));
            _itemDisplay = dRt;
            return area.gameObject;
        }

        private void GenerateItemAndSave()
        {
            Item it = Lab6ItemGenerator.Generate(_save.nextItemId++);
            _save.items.Add(it);
            Lab6SaveSystem.Save(_save);
            _currentItem = it;
            RenderItemDisplay();
        }

        private void RenderItemDisplay()
        {
            ClearChildren(_itemDisplay);
            if (_currentItem == null)
            {
                Lab6Ui.Label(_itemDisplay, "Press <b>Generate Item</b> to roll a new item.", 14, TextAnchor.MiddleCenter);
                _itemNameText = null;
                return;
            }

            GameObject card = Lab6Ui.Panel(_itemDisplay, "Card", new Color(0.10f, 0.11f, 0.16f));
            RectTransform cRt = (RectTransform)card.transform;
            Lab6Ui.Anchor(cRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320, -220), new Vector2(320, 220));

            VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 6;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;

            Color rcol = Lab6Colors.For(_currentItem.rarity);
            string colorHex = ColorUtility.ToHtmlStringRGB(rcol);
            string stars = Lab6ItemGenerator.Stars(_currentItem.rarity);

            _itemNameText = Lab6Ui.Label(card.transform,
                $"<color=#{colorHex}><b>{_currentItem.name}</b></color>  <color=#{colorHex}>{stars}</color>",
                26, TextAnchor.MiddleCenter, FontStyle.Bold);

            Lab6Ui.Label(card.transform,
                $"<color=#{colorHex}>{_currentItem.rarity}</color>  -  {_currentItem.type}",
                16, TextAnchor.MiddleCenter);
            Lab6Ui.Label(card.transform, "", 4);
            Lab6Ui.Label(card.transform, $"Damage:     {Mathf.RoundToInt(_currentItem.damage)}", 16, TextAnchor.MiddleCenter);
            Lab6Ui.Label(card.transform, $"Durability: {Mathf.RoundToInt(_currentItem.durability)}", 16, TextAnchor.MiddleCenter);

            if (_currentItem.abilities != null && _currentItem.abilities.Count > 0)
            {
                Lab6Ui.Label(card.transform, "", 4);
                Lab6Ui.Label(card.transform, "<b>Special Abilities</b>", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
                foreach (Ability a in _currentItem.abilities)
                {
                    Lab6Ui.Label(card.transform, $"<color=#{colorHex}>- {a.Format()}</color>", 14, TextAnchor.MiddleCenter);
                }
            }
        }

        private void Update()
        {
            // Legendary item animation (Bonus 2)
            if (_itemNameText != null && _currentItem != null && _currentItem.rarity == Rarity.Legendary)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
                Color a = Lab6Colors.Legendary;
                Color b = new Color(1f, 0.95f, 0.55f);
                Color c = Color.Lerp(a, b, t);
                string hex = ColorUtility.ToHtmlStringRGB(c);
                string stars = Lab6ItemGenerator.Stars(Rarity.Legendary);
                _itemNameText.text = $"<color=#{hex}><b>{_currentItem.name}</b></color>  <color=#{hex}>{stars}</color>";
                float scale = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 3.2f);
                _itemNameText.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // ====================== WORLD TAB ======================
        private GameObject BuildWorldPanel()
        {
            RectTransform area = MakePanelArea("WorldPanel");

            GameObject actions = Lab6Ui.Panel(area, "Actions", new Color(0.14f, 0.16f, 0.22f));
            RectTransform actRt = (RectTransform)actions.transform;
            Lab6Ui.Anchor(actRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -52), new Vector2(-8, -8));
            HorizontalLayoutGroup hlg = actions.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            AddSizedButton(actions.transform, "New Simulation", () => StartWorld(5), new Color(0.35f, 0.55f, 0.30f), 160);
            AddSizedButton(actions.transform, "Advance Day",   AdvanceWorldDay,      new Color(0.40f, 0.45f, 0.65f), 160);

            _worldStatusText = Lab6Ui.Label(actions.transform, "No simulation running.", 14, TextAnchor.MiddleLeft);

            // Map (left side, top)
            GameObject map = Lab6Ui.Panel(area, "Map", new Color(0.12f, 0.13f, 0.18f));
            RectTransform mRt = (RectTransform)map.transform;
            Lab6Ui.Anchor(mRt, new Vector2(0, 0), new Vector2(0.55f, 1), new Vector2(8, 8), new Vector2(-4, -60));
            _mapRoot = mRt;

            // Journal (right side)
            GameObject journalWrapper = Lab6Ui.Panel(area, "JournalWrap", new Color(0.13f, 0.14f, 0.20f));
            RectTransform jRt = (RectTransform)journalWrapper.transform;
            Lab6Ui.Anchor(jRt, new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(4, 8), new Vector2(-8, -60));

            Text jHeader = Lab6Ui.Label(journalWrapper.transform, "<b>Journal</b>", 16, TextAnchor.UpperLeft, FontStyle.Bold);
            RectTransform jHRt = jHeader.GetComponent<RectTransform>();
            Lab6Ui.Anchor(jHRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -26), new Vector2(-10, -6));

            ScrollRect sr = Lab6Ui.ScrollView(journalWrapper.transform, out RectTransform jContent, new Color(0.08f, 0.09f, 0.13f));
            RectTransform svRt = sr.GetComponent<RectTransform>();
            Lab6Ui.Anchor(svRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -32));
            _journalContent = jContent;

            return area.gameObject;
        }

        private void AddSizedButton(Transform parent, string label, System.Action onClick, Color color, float width)
        {
            Button b = Lab6Ui.Btn(parent, label, onClick, color);
            LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.minWidth = width;
        }

        private void StartWorld(int npcCount)
        {
            _world = new Lab6World();
            _world.StartNew(npcCount, System.Environment.TickCount);
            RefreshWorldUi();
        }

        private void AdvanceWorldDay()
        {
            if (_world == null) StartWorld(5);
            if (_world.Finished) return;
            _world.AdvanceDay();
            RefreshWorldUi();
        }

        private void RefreshWorldUi()
        {
            if (_journalContent == null || _mapRoot == null) return;

            if (_world == null)
            {
                _worldStatusText.text = "No simulation. Press <b>New Simulation</b>.";
                ClearChildren(_journalContent);
                ClearChildren(_mapRoot);
                Lab6Ui.Label(_mapRoot, "Map appears once a simulation starts.", 14, TextAnchor.MiddleCenter);
                return;
            }

            int alive = 0;
            foreach (Npc n in _world.Npcs) if (n.alive) alive++;
            string status = _world.Finished
                ? $"<b>FINISHED</b> on Day {_world.Day} - alive: {alive}/{_world.Npcs.Count}"
                : $"Day {_world.Day} / {Lab6World.MaxDays}  -  alive: {alive}/{_world.Npcs.Count}";
            _worldStatusText.text = status;

            RebuildJournal();
            RebuildMap();
        }

        private void RebuildJournal()
        {
            ClearChildren(_journalContent);
            foreach (string line in _world.Journal)
            {
                Lab6Ui.Label(_journalContent, line, 13);
            }
            if (_world.Finished && !string.IsNullOrEmpty(_world.FinalSummary))
            {
                Lab6Ui.Label(_journalContent, "", 6);
                Lab6Ui.Label(_journalContent, $"<color=#ffd66b><b>{_world.FinalSummary.Replace("\n", "</b>\n<b>")}</b></color>", 13);
            }

            Canvas.ForceUpdateCanvases();
            ScrollRect sr = _journalContent.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 0f;
        }

        private void RebuildMap()
        {
            ClearChildren(_mapRoot);

            Text header = Lab6Ui.Label(_mapRoot, "<b>Realm Map</b>", 16, TextAnchor.UpperLeft, FontStyle.Bold);
            Lab6Ui.Anchor(header.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -26), new Vector2(-10, -6));

            GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(_mapRoot, false);
            RectTransform gRt = (RectTransform)grid.transform;
            Lab6Ui.Anchor(gRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -34));

            GridLayoutGroup gl = grid.GetComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(210, 180);
            gl.spacing = new Vector2(8, 8);
            gl.padding = new RectOffset(4, 4, 4, 4);
            gl.startAxis = GridLayoutGroup.Axis.Horizontal;
            gl.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;

            for (int i = 0; i < _world.Locations.Count; i++)
            {
                LocationType loc = _world.Locations[i];
                Color baseColor = Lab6Colors.ForLocation(loc);
                GameObject cell = Lab6Ui.Panel(grid.transform, loc.ToString(), Color.Lerp(baseColor, Color.black, 0.45f));

                VerticalLayoutGroup cvlg = cell.AddComponent<VerticalLayoutGroup>();
                cvlg.padding = new RectOffset(8, 8, 6, 6);
                cvlg.spacing = 2;
                cvlg.childForceExpandWidth = true;
                cvlg.childForceExpandHeight = false;
                cvlg.childControlHeight = true;

                Lab6Ui.Label(cell.transform, $"<b>{loc}</b>", 14, TextAnchor.UpperLeft, FontStyle.Bold);

                int populated = 0;
                foreach (Npc n in _world.Npcs)
                {
                    if (n.locationIndex != i) continue;
                    string colorHex = ColorUtility.ToHtmlStringRGB(Lab6Colors.ForClass(n.cls));
                    string state = n.alive ? "" : " <color=#999999>(dead)</color>";
                    string hp = $" <color=#aaaaaa>[{Mathf.RoundToInt(n.hp)}/{Mathf.RoundToInt(n.maxHp)}]</color>";
                    Lab6Ui.Label(cell.transform, $"<color=#{colorHex}>{n.name}</color>{hp}{state}", 12);
                    populated++;
                }
                if (populated == 0)
                {
                    Lab6Ui.Label(cell.transform, "<i>empty</i>", 11, TextAnchor.UpperLeft, FontStyle.Italic);
                }
            }
        }

        // ====================== GALLERY TAB ======================
        private GameObject BuildGalleryPanel()
        {
            RectTransform area = MakePanelArea("GalleryPanel");

            GameObject actions = Lab6Ui.Panel(area, "Actions", new Color(0.14f, 0.16f, 0.22f));
            RectTransform actRt = (RectTransform)actions.transform;
            Lab6Ui.Anchor(actRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -52), new Vector2(-8, -8));
            HorizontalLayoutGroup hlg = actions.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 8;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            AddSizedButton(actions.transform, "Show NPCs",  () => { _galleryShowItems = false; RefreshGallery(); }, new Color(0.30f, 0.45f, 0.30f), 120);
            AddSizedButton(actions.transform, "Show Items", () => { _galleryShowItems = true;  RefreshGallery(); }, new Color(0.40f, 0.40f, 0.65f), 120);
            AddSizedButton(actions.transform, "Clear Save", ClearSave, new Color(0.55f, 0.30f, 0.30f), 120);
            Lab6Ui.Label(actions.transform, $"Save: {Lab6SaveSystem.GetSavePath()}", 11, TextAnchor.MiddleLeft);

            GameObject scrollWrap = Lab6Ui.Panel(area, "ScrollWrap", new Color(0.13f, 0.14f, 0.20f));
            RectTransform swRt = (RectTransform)scrollWrap.transform;
            Lab6Ui.Anchor(swRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(8, 8), new Vector2(-8, -60));

            ScrollRect sr = Lab6Ui.ScrollView(scrollWrap.transform, out RectTransform gallery, new Color(0.08f, 0.09f, 0.13f));
            Lab6Ui.Stretch(sr.gameObject, 6);
            _galleryContent = gallery;

            return area.gameObject;
        }

        private void ClearSave()
        {
            _save = new SaveData();
            Lab6SaveSystem.Save(_save);
            RefreshGallery();
        }

        private void RefreshGallery()
        {
            if (_galleryContent == null) return;
            ClearChildren(_galleryContent);

            if (!_galleryShowItems)
            {
                if (_save.npcs.Count == 0)
                {
                    Lab6Ui.Label(_galleryContent, "<i>No NPCs yet. Use the NPC tab to roll some.</i>", 14, TextAnchor.MiddleCenter, FontStyle.Italic);
                    return;
                }
                foreach (Npc n in _save.npcs) BuildNpcRow(n);
            }
            else
            {
                if (_save.items.Count == 0)
                {
                    Lab6Ui.Label(_galleryContent, "<i>No items yet. Use the Item tab to roll some.</i>", 14, TextAnchor.MiddleCenter, FontStyle.Italic);
                    return;
                }
                foreach (Item it in _save.items) BuildItemRow(it);
            }
        }

        private void BuildNpcRow(Npc n)
        {
            GameObject row = Lab6Ui.Panel(_galleryContent, "Row", new Color(0.12f, 0.13f, 0.18f));
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 64; le.minHeight = 64;

            GameObject portrait = Lab6Ui.Panel(row.transform, "Portrait", Lab6Colors.ForClass(n.cls));
            RectTransform pRt = (RectTransform)portrait.transform;
            Lab6Ui.Anchor(pRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 6), new Vector2(0, -6));
            pRt.sizeDelta = new Vector2(52, 0);
            Text init = Lab6Ui.Label(portrait.transform, n.cls.ToString().Substring(0, 1), 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            Lab6Ui.Stretch(init.gameObject);

            // Info
            Text info = Lab6Ui.Label(row.transform,
                $"<b>{n.name}</b>  <color=#aaaaaa>({n.cls})</color>\n" +
                $"HP {Mathf.RoundToInt(n.hp)}/{Mathf.RoundToInt(n.maxHp)}   DMG {Mathf.RoundToInt(n.damage)}   ARM {Mathf.RoundToInt(n.armor)}   <i>{n.trait}</i>",
                13, TextAnchor.MiddleLeft);
            RectTransform iRt = info.GetComponent<RectTransform>();
            Lab6Ui.Anchor(iRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(66, 6), new Vector2(-260, -6));

            // Rename
            InputField inp = Lab6Ui.Input(row.transform, "rename...");
            RectTransform inpRt = (RectTransform)inp.transform;
            Lab6Ui.Anchor(inpRt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-250, 12), new Vector2(-90, -12));
            int id = n.id;
            Button apply = Lab6Ui.Btn(row.transform, "Rename", () =>
            {
                if (!string.IsNullOrWhiteSpace(inp.text))
                {
                    Npc target = _save.npcs.Find(x => x.id == id);
                    if (target != null) target.name = inp.text.Trim();
                    Lab6SaveSystem.Save(_save);
                    RefreshGallery();
                }
            }, new Color(0.30f, 0.45f, 0.55f));
            RectTransform aRt = (RectTransform)apply.transform;
            Lab6Ui.Anchor(aRt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-84, 12), new Vector2(-6, -12));
        }

        private void BuildItemRow(Item it)
        {
            GameObject row = Lab6Ui.Panel(_galleryContent, "Row", new Color(0.12f, 0.13f, 0.18f));
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 64; le.minHeight = 64;

            GameObject swatch = Lab6Ui.Panel(row.transform, "Swatch", Lab6Colors.For(it.rarity));
            RectTransform sRt = (RectTransform)swatch.transform;
            Lab6Ui.Anchor(sRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 6), new Vector2(0, -6));
            sRt.sizeDelta = new Vector2(14, 0);

            string colorHex = ColorUtility.ToHtmlStringRGB(Lab6Colors.For(it.rarity));
            string stars = Lab6ItemGenerator.Stars(it.rarity);
            string abilities = it.abilities != null && it.abilities.Count > 0
                ? string.Join(", ", it.abilities.ConvertAll(a => a.Format()))
                : "-";

            Text info = Lab6Ui.Label(row.transform,
                $"<color=#{colorHex}><b>{it.name}</b> {stars}</color>  <color=#aaaaaa>({it.type})</color>\n" +
                $"DMG {Mathf.RoundToInt(it.damage)}   DUR {Mathf.RoundToInt(it.durability)}   <i>{abilities}</i>",
                13, TextAnchor.MiddleLeft);
            RectTransform iRt = info.GetComponent<RectTransform>();
            Lab6Ui.Anchor(iRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 6), new Vector2(-260, -6));

            InputField inp = Lab6Ui.Input(row.transform, "rename...");
            RectTransform inpRt = (RectTransform)inp.transform;
            Lab6Ui.Anchor(inpRt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-250, 12), new Vector2(-90, -12));
            int id = it.id;
            Button apply = Lab6Ui.Btn(row.transform, "Rename", () =>
            {
                if (!string.IsNullOrWhiteSpace(inp.text))
                {
                    Item target = _save.items.Find(x => x.id == id);
                    if (target != null) target.name = inp.text.Trim();
                    Lab6SaveSystem.Save(_save);
                    RefreshGallery();
                }
            }, new Color(0.30f, 0.45f, 0.55f));
            RectTransform aRt = (RectTransform)apply.transform;
            Lab6Ui.Anchor(aRt, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-84, 12), new Vector2(-6, -12));
        }

        // ====================== Helpers ======================
        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject child = t.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }
    }
}
