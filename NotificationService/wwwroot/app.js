// Notification Dashboard Application
let connection;
let notifications = [];
let orderCount = 0;

// Initialize the application
document.addEventListener('DOMContentLoaded', async () => {
    await loadHistoricalNotifications();
    setupSignalRConnection();
    setupEventListeners();
});

// Setup SignalR connection for real-time notifications
function setupSignalRConnection() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveNotification", (notification) => {
        addNotification(notification);
    });

    connection.onreconnecting(() => {
        updateConnectionStatus(false);
    });

    connection.onreconnected(() => {
        updateConnectionStatus(true);
    });

    connection.onclose(() => {
        updateConnectionStatus(false);
        // Attempt to reconnect after 5 seconds
        setTimeout(() => startConnection(), 5000);
    });

    startConnection();
}

// Start SignalR connection
async function startConnection() {
    try {
        await connection.start();
        updateConnectionStatus(true);
        console.log("SignalR connected");
    } catch (err) {
        console.error("Error connecting to SignalR:", err);
        updateConnectionStatus(false);
        // Retry after 5 seconds
        setTimeout(() => startConnection(), 5000);
    }
}

// Update connection status UI
function updateConnectionStatus(connected) {
    const statusDot = document.getElementById('connection-status');
    const statusText = document.getElementById('connection-text');
    
    if (connected) {
        statusDot.className = 'status-dot connected';
        statusText.textContent = 'Connected';
    } else {
        statusDot.className = 'status-dot disconnected';
        statusText.textContent = 'Disconnected';
    }
}

// Load historical notifications from the server
async function loadHistoricalNotifications() {
    try {
        const response = await fetch('/notifications/history');
        if (response.ok) {
            const history = await response.json();
            notifications = history;
            history.forEach(notification => {
                addNotificationToUI(notification);
                updateStats(notification.type);
            });
        }
    } catch (err) {
        console.error("Error loading historical notifications:", err);
    }
}

// Add a notification to the list
function addNotification(notification) {
    notifications.unshift(notification);
    addNotificationToUI(notification);
    updateStats(notification.type);
}

// Add notification to the UI
function addNotificationToUI(notification) {
    const container = document.getElementById('notifications-container');
    
    // Remove empty state if it exists
    const emptyState = container.querySelector('.empty-state');
    if (emptyState) {
        emptyState.remove();
    }

    const card = createNotificationCard(notification);
    container.insertBefore(card, container.firstChild);
}

// Get emoji for notification based on type and status
function getNotificationEmoji(notification) {
    // For order notifications, check the status
    const status = notification.metadata?.Status?.toLowerCase() || '';
    
    switch (status) {
        case 'created':
            return '🆕';
        case 'payment_processed':
            return '💳';
        case 'shipped':
            return '🚚';
        case 'delivered':
            return '📦';
        case 'completed':
            return '✅';
        case 'failed':
            return '❌';
        default:
            return '📋';
    }
}

// Create a notification card element
function createNotificationCard(notification) {
    const card = document.createElement('div');
    card.className = 'notification-card';
    
    const header = document.createElement('div');
    header.className = 'notification-header';
    
    const emoji = getNotificationEmoji(notification);
    const typeSpan = document.createElement('span');
    typeSpan.className = `notification-type ${notification.type}`;
    typeSpan.innerHTML = `${emoji} ${notification.type}`;
    
    const timeSpan = document.createElement('span');
    timeSpan.className = 'notification-time';
    timeSpan.textContent = formatTime(notification.timestamp);
    
    header.appendChild(typeSpan);
    header.appendChild(timeSpan);
    
    const title = document.createElement('div');
    title.className = 'notification-title';
    title.textContent = notification.title;
    
    const message = document.createElement('div');
    message.className = 'notification-message';
    message.textContent = notification.message;
    
    card.appendChild(header);
    card.appendChild(title);
    card.appendChild(message);
    
    // Add metadata if available
    if (notification.metadata && Object.keys(notification.metadata).length > 0) {
        const metadataContainer = document.createElement('div');
        metadataContainer.className = 'notification-metadata';
        
        for (const [key, value] of Object.entries(notification.metadata)) {
            const metadataItem = document.createElement('div');
            metadataItem.className = 'metadata-item';
            metadataItem.innerHTML = `
                <span class="metadata-label">${key}:</span>
                <span class="metadata-value">${value}</span>
            `;
            metadataContainer.appendChild(metadataItem);
        }
        
        card.appendChild(metadataContainer);
    }
    
    return card;
}

// Format timestamp
function formatTime(timestamp) {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} min${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    
    return date.toLocaleString();
}

// Update statistics
function updateStats(type) {
    if (type === 'order') {
        orderCount++;
        document.getElementById('order-notifications').textContent = orderCount;
    }
    
    document.getElementById('total-notifications').textContent = notifications.length;
}

// Clear all notifications
function clearNotifications() {
    notifications = [];
    orderCount = 0;
    
    const container = document.getElementById('notifications-container');
    container.innerHTML = `
        <div class="empty-state">
            <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
            </svg>
            <p>No notifications yet</p>
            <p class="empty-subtitle">Waiting for order events...</p>
        </div>
    `;
    
    document.getElementById('total-notifications').textContent = '0';
    document.getElementById('order-notifications').textContent = '0';
}

// Create a new order (replaces test order notification)
function showOrderModal() {
    const modal = document.getElementById('order-modal');
    modal.classList.add('show');
}

function hideOrderModal() {
    const modal = document.getElementById('order-modal');
    modal.classList.remove('show');
    document.getElementById('order-form').reset();
    
    // Reset to one item row
    const itemsContainer = document.getElementById('items-container');
    const itemRows = itemsContainer.querySelectorAll('.item-row');
    itemRows.forEach((row, index) => {
        if (index > 0) row.remove();
    });
}

function addItemRow() {
    const itemsContainer = document.getElementById('items-container');
    const itemRow = document.createElement('div');
    itemRow.className = 'item-row';
    itemRow.innerHTML = `
        <div class="form-group">
            <label>Product ID</label>
            <input type="text" name="productId" placeholder="e.g., prod-001" required>
        </div>
        <div class="form-group">
            <label>Quantity</label>
            <input type="number" name="quantity" min="1" value="1" required>
        </div>
        <div class="form-group">
            <label>Price</label>
            <input type="number" name="price" min="0" step="0.01" placeholder="29.99" required>
        </div>
        <button type="button" class="btn-remove-item" title="Remove item">×</button>
    `;
    
    // Add remove handler
    itemRow.querySelector('.btn-remove-item').addEventListener('click', () => {
        itemRow.remove();
    });
    
    itemsContainer.appendChild(itemRow);
}

async function submitOrderForm(event) {
    event.preventDefault();
    
    const form = event.target;
    const formData = new FormData(form);
    
    // Get customer ID
    const customerId = formData.get('customerId');
    
    // Get all items
    const productIds = formData.getAll('productId');
    const quantities = formData.getAll('quantity');
    const prices = formData.getAll('price');
    
    const items = productIds.map((productId, index) => ({
        productId: productId,
        quantity: parseInt(quantities[index]),
        price: parseFloat(prices[index])
    }));
    
    const orderRequest = {
        orderId: null,
        customerId: customerId,
        items: items
    };
    
    try {
        console.log('Creating order:', orderRequest);
        
        const response = await fetch('/order', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(orderRequest)
        });
        
        if (response.ok) {
            const result = await response.json();
            console.log('Order created successfully:', result);
            
            // Hide modal
            hideOrderModal();
        } else {
            const error = await response.text();
            console.error('Failed to create order:', error);
            alert('Failed to create order: ' + error);
        }
    } catch (error) {
        console.error('Error creating order:', error);
        alert('Error creating order: ' + error.message);
    }
}

// Generate unique ID
function generateId() {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

// Setup event listeners
function setupEventListeners() {
    document.getElementById('clear-btn').addEventListener('click', clearNotifications);
    document.getElementById('create-order-btn').addEventListener('click', showOrderModal);
    
    // Modal event listeners
    document.getElementById('close-modal-btn').addEventListener('click', hideOrderModal);
    document.getElementById('cancel-order-btn').addEventListener('click', hideOrderModal);
    document.getElementById('add-item-btn').addEventListener('click', addItemRow);
    document.getElementById('order-form').addEventListener('submit', submitOrderForm);
    
    // Close modal on outside click
    document.getElementById('order-modal').addEventListener('click', (e) => {
        if (e.target.id === 'order-modal') {
            hideOrderModal();
        }
    });
    
    // Setup remove button for initial item row
    document.querySelector('.btn-remove-item').addEventListener('click', function() {
        const itemsContainer = document.getElementById('items-container');
        const itemRows = itemsContainer.querySelectorAll('.item-row');
        if (itemRows.length > 1) {
            this.closest('.item-row').remove();
        } else {
            alert('You must have at least one item in the order.');
        }
    });
}
