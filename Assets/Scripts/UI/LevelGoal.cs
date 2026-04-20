using UnityEngine;
using TMPro;

// tiny goal loop: press E near a coin, see counter/messages, win screen on the last one
// (всё в одном скрипте, чтобы не плодить менеджеров)
public class LevelGoal : MonoBehaviour
{
    // drag GridManager here; Player is auto-found through grid.player at Start
    public GridManager grid;
    public Player player;

    // UI bits — cоздаются в сцене руками, просто перетащить в инспектор
    public TMP_Text counterText;      // "Coins: X / Y"
    public TMP_Text messageText;      // "Find all coins", "You found a coin"
    public GameObject completionPanel; // тёмная панель с большим "Completed!"

    // how long the little message stays on screen
    public float messageSeconds = 2f;

    int picked;
    int total;
    float messageTimer;
    bool completed;
    bool initialized;

    void Update()
    {
        // первый кадр — подхватываем игрока и общее число монет
        // так мы не зависим от порядка Start() между этим скриптом и GridManager
        if (!initialized)
        {
            if (grid == null) return;
            if (player == null) player = grid.player; // auto-find, меньше ручной связки
            if (player == null) return; // ждём пока grid его заспавнит

            total = grid.LootCount;
            picked = 0;
            UpdateCounter();
            ShowMessage("Find all coins");
            if (completionPanel != null) completionPanel.SetActive(false);
            initialized = true;
            return; // одну кнопку E на этом же кадре не ловим, а то ложный pickup
        }

        // таймер для короткого сообщения — через пару секунд просто стираем
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText != null)
                messageText.text = "";
        }

        if (completed) return; // after win — ignore E

        // PCG: collectible pickup
        if (Input.GetKeyDown(KeyCode.E))
            TryPickupAround();
    }

    // smotrim на свою клетку и 8 соседей, забираем первую найденную монетку
    void TryPickupAround()
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = player.gridX + dx;
                int ny = player.gridY + dy;
                if (grid.TryPickupLootAt(nx, ny))
                {
                    picked++;
                    ShowMessage("You found a coin");
                    UpdateCounter();
                    if (picked >= total) ShowCompletion();
                    return; // one coin per press — так приятнее ヽ(・∀・)ﾉ
                }
            }
        }
    }

    // PCG: collectible counter
    void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = "Coins: " + picked + " / " + total;
    }

    void ShowMessage(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageTimer = messageSeconds;
    }

    // PCG: level completion
    void ShowCompletion()
    {
        completed = true;
        if (completionPanel != null) completionPanel.SetActive(true);
    }
}
