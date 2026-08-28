(function () {
    var toggle = document.getElementById('chatToggle');
    var panel = document.getElementById('chatPanel');
    var closeBtn = document.getElementById('chatClose');
    var form = document.getElementById('chatForm');
    var input = document.getElementById('chatInput');
    var messagesEl = document.getElementById('chatMessages');
    if (!toggle) return; // widget not present (e.g. not logged in)

    var history = []; // { role: 'user'|'assistant', content: string }

    toggle.addEventListener('click', function () { panel.classList.toggle('d-none'); });
    closeBtn.addEventListener('click', function () { panel.classList.add('d-none'); });

    function render() {
        messagesEl.innerHTML = '';
        history.forEach(function (turn) {
            var div = document.createElement('div');
            div.className = 'chat-msg ' + turn.role;
            div.textContent = turn.content;
            messagesEl.appendChild(div);
        });
        messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        var text = input.value.trim();
        if (!text) return;

        history.push({ role: 'user', content: text });
        input.value = '';
        render();

        var thinking = document.createElement('div');
        thinking.className = 'chat-msg assistant';
        thinking.textContent = '...';
        messagesEl.appendChild(thinking);
        messagesEl.scrollTop = messagesEl.scrollHeight;

        try {
            var response = await fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ history: history })
            });
            var data = await response.json();
            thinking.remove();
            history.push({ role: 'assistant', content: data.reply || 'Something went wrong.' });
        } catch (err) {
            thinking.remove();
            history.push({ role: 'assistant', content: 'Could not reach the assistant.' });
        }
        render();
    });
})();
