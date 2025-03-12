import axios from 'axios';
import { marked } from 'marked';
import DOMPurify from 'dompurify';
import { ChatMessage, ChatRequest, ChatResponse } from './types';

export class App {
  private chatMessages: HTMLElement | null = null;
  private chatInput: HTMLInputElement | null = null;
  private sendButton: HTMLButtonElement | null = null;
  private chatForm: HTMLFormElement | null = null;
  private messageHistory: ChatMessage[] = [];
  private isWaitingForResponse: boolean = false;
  private username: string | null = null;

  public async init(): Promise<void> {
    // Extract username from query string if present
    this.username = this.getUsernameFromUrl();
    const modelName = await this.fetchModelName();
    this.render(modelName);
    this.bindElements();
    this.bindEvents();
  }

  private getUsernameFromUrl(): string | null {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('username');
  }

  private render(modelName: string): void {
    const app = document.getElementById('app');
    if (!app) return;

    app.innerHTML = `
      <header class="header">
        <div class="header-logo">
          <img src="./assets/azure-app-configuration-icon.svg" alt="Azure App Configuration Logo" />
          <h1 class="header-title">Azure App Configuration AI Chat</h1>
        </div>
        ${this.username ? `<div class="user-badge">${this.username}</div>` : ''}
      </header>
      <main class="chat-container">
        <div class="chat-messages" id="chat-messages">
          <div class="welcome-container">
            <img src="./assets/azure-app-configuration-icon.svg" alt="Azure App Configuration Logo" class="welcome-logo" />
            <h2 class="welcome-title">Welcome ${this.username ? `${this.username}` : ''} to Azure App Configuration AI Chat</h2>
            <p class="welcome-description">
              I'm your AI assistant powered by Azure App Configuration (model: ${modelName}). Ask me anything and I'll do my best to help you.
            </p>
          </div>
        </div>
        <div class="chat-input-container">
          <form id="chat-form" class="chat-input-form">
            <input 
              type="text" 
              id="chat-input" 
              class="chat-input" 
              placeholder="Type your message..." 
              autocomplete="off"
            />
            <button type="submit" id="send-button" class="send-button">Send</button>
          </form>
        </div>
      </main>
    `;
  }

  private bindElements(): void {
    this.chatMessages = document.getElementById('chat-messages');
    this.chatInput = document.getElementById('chat-input') as HTMLInputElement;
    this.sendButton = document.getElementById('send-button') as HTMLButtonElement;
    this.chatForm = document.getElementById('chat-form') as HTMLFormElement;
  }

  private bindEvents(): void {
    if (this.chatForm) {
      this.chatForm.addEventListener('submit', (e) => {
        e.preventDefault();
        this.handleSendMessage();
      });
    }

    if (this.chatInput) {
      this.chatInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          this.handleSendMessage();
        }
      });

      // Enable/disable send button based on input
      this.chatInput.addEventListener('input', () => {
        if (this.sendButton) {
          this.sendButton.disabled = !this.chatInput?.value.trim();
        }
      });
    }
  }

  private async fetchModelName(): Promise<string> {
    try {
      const url = `/api/chat/model?username=${this.username}`;
      const response = await axios.get<string>(url);
      return response.data;
    } catch (error) {
      console.error('Error fetching model name:', error);
      return 'unknown';
    }
  }

  private handleSendMessage(): void {
    if (!this.chatInput || !this.sendButton || this.isWaitingForResponse) return;

    const message = this.chatInput.value.trim();
    if (!message) return;

    // Clear input and disable button
    this.chatInput.value = '';
    this.sendButton.disabled = true;
    this.isWaitingForResponse = true;

    // Add user message to UI
    this.addMessageToUI('user', message);

    // Add user message to history
    this.messageHistory.push({
      role: 'user',
      content: message,
      timestamp: new Date()
    });

    // Show typing indicator
    this.showTypingIndicator();

    // Send message to API
    this.sendMessageToAPI(message);
  }

  private async sendMessageToAPI(message: string): Promise<void> {
    try {
      const request: ChatRequest = {
        message,
        history: this.messageHistory
      };

      const url = `/api/chat?username=${this.username}`;
      const response = await axios.post<ChatResponse>(url, request);
      
      // Hide typing indicator
      this.hideTypingIndicator();

      // Update message history with the complete history from response
      this.messageHistory = response.data.history;

      // Add bot message to UI
      this.addMessageToUI('bot', response.data.message);
      
    } catch (error) {
      console.error('Error sending message:', error);
      
      // Hide typing indicator
      this.hideTypingIndicator();
      
      // Show error message
      this.addMessageToUI('bot', 'Sorry, I encountered an error. Please try again later.');
    } finally {
      this.isWaitingForResponse = false;
      if (this.sendButton) {
        this.sendButton.disabled = false;
      }
      if (this.chatInput) {
        this.chatInput.focus();
      }
    }
  }

  private addMessageToUI(role: 'user' | 'bot', content: string): void {
    if (!this.chatMessages) return;

    const messageElement = document.createElement('div');
    messageElement.className = `message ${role}`;

    // Format the message content (handle markdown for bot messages)
    let formattedContent = content;
    if (role === 'bot') {
      // Parse markdown and sanitize HTML
      formattedContent = DOMPurify.sanitize(marked(content));
    }

    const timestamp = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    messageElement.innerHTML = `
      <div class="message-bubble">${role === 'bot' ? formattedContent : content}</div>
      <div class="message-timestamp">${timestamp}</div>
    `;

    this.chatMessages.appendChild(messageElement);
    
    // Scroll to bottom
    this.scrollToBottom();
  }

  private showTypingIndicator(): void {
    if (!this.chatMessages) return;

    const typingIndicator = document.createElement('div');
    typingIndicator.id = 'typing-indicator';
    typingIndicator.className = 'message bot';
    typingIndicator.innerHTML = `
      <div class="typing-indicator">
        <div class="typing-dot"></div>
        <div class="typing-dot"></div>
        <div class="typing-dot"></div>
      </div>
    `;

    this.chatMessages.appendChild(typingIndicator);
    this.scrollToBottom();
  }

  private hideTypingIndicator(): void {
    const typingIndicator = document.getElementById('typing-indicator');
    if (typingIndicator) {
      typingIndicator.remove();
    }
  }

  private scrollToBottom(): void {
    if (this.chatMessages) {
      this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
    }
  }
}