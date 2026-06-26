const BASE = '/api';

export interface Product { sku: string; name: string; description: string; price: number; imageUrl: string; }
export interface BasketItem { sku: string; name: string; unitPrice: number; quantity: number; }
export interface PurchaseIntentState {
  purchaseIntentId: string;
  items: BasketItem[];
  email: string | null;
  emailVerified: boolean;
  status: number;
  checkoutId: string | null;
  createdAt: string;
}
export interface ShippingAddress { fullName: string; line1: string; line2?: string; city: string; postalCode: string; country: string; }
export interface PaymentMethod { cardHolder: string; maskedNumber: string; expiryMonth: string; expiryYear: string; }
export interface CheckoutState {
  checkoutId: string;
  status: number;
  failureReason: string | null;
  shippingAddress: ShippingAddress | null;
  payment: PaymentMethod | null;
  emailVerified: boolean;
}

export const CheckoutStatusLabel: Record<number, string> = {
  0: 'Waiting for conditions',
  1: 'Processing inventory',
  2: 'Processing payment',
  3: 'Processing fulfillment',
  4: 'Completed',
  5: 'Failed',
  6: 'Cancelled',
  7: 'Payment Declined — awaiting retry',
};

const json = async <T>(res: Response): Promise<T> => {
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  if (res.status === 204) {
    return undefined as T;
  }
  return res.json() as Promise<T>;
};

export const api = {
  getProducts: () => fetch(`${BASE}/products`, { credentials: 'include' }).then((r) => json<Product[]>(r)),
  getSession: () => fetch(`${BASE}/session`, { credentials: 'include' }).then((r) => json<{ purchaseIntentId: string | null }>(r)),
  newCleanCustomer: () => fetch(`${BASE}/session/new-clean-customer`, { method: 'POST', credentials: 'include' }).then((r) => json<{ purchaseIntentId: string }>(r)),
  clearCookie: () => fetch(`${BASE}/session/clear-cookie`, { method: 'POST', credentials: 'include' }).then((r) => json<unknown>(r)),
  getPurchaseIntent: () => fetch(`${BASE}/purchase-intent/current`, { credentials: 'include' }).then((r) => json<PurchaseIntentState>(r)),
  addItem: (sku: string, quantity: number) => fetch(`${BASE}/purchase-intent/items`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sku, quantity }) }).then((r) => json<unknown>(r)),
  updateQuantity: (sku: string, quantity: number) => fetch(`${BASE}/purchase-intent/items/${sku}`, { method: 'PUT', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ quantity }) }).then((r) => json<unknown>(r)),
  removeItem: (sku: string) => fetch(`${BASE}/purchase-intent/items/${sku}`, { method: 'DELETE', credentials: 'include' }).then((r) => json<unknown>(r)),
  provideEmail: (email: string) => fetch(`${BASE}/purchase-intent/email`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email }) }).then((r) => json<{ verificationId: string }>(r)),
  startCheckout: () => fetch(`${BASE}/purchase-intent/checkout`, { method: 'POST', credentials: 'include' }).then((r) => json<{ checkoutId: string }>(r)),
  getCheckout: (checkoutId: string) => fetch(`${BASE}/checkouts/${checkoutId}`, { credentials: 'include' }).then((r) => json<CheckoutState>(r)),
  provideShipping: (checkoutId: string, address: ShippingAddress) => fetch(`${BASE}/checkouts/${checkoutId}/shipping-address`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ address }) }).then((r) => json<unknown>(r)),
  providePayment: (checkoutId: string, payment: PaymentMethod) => fetch(`${BASE}/checkouts/${checkoutId}/payment-method`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ payment }) }).then((r) => json<unknown>(r)),
  retryPayment: (checkoutId: string) => fetch(`${BASE}/checkouts/${checkoutId}/retry-payment`, { method: 'POST', credentials: 'include' }).then((r) => json<unknown>(r)),
  cancelCheckout: (checkoutId: string) => fetch(`${BASE}/checkouts/${checkoutId}/cancel`, { method: 'POST', credentials: 'include' }).then((r) => json<unknown>(r)),
};
