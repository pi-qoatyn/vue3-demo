import { defineStore } from 'pinia'
import { useLocalStorage } from '@vueuse/core'

export const useUserStore = defineStore({
    id: "use-info",
    state: () => ({
        u_info: {},
        n: useLocalStorage('key', 0)
    }),
    persist: true
})