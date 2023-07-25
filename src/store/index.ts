import { defineStore } from 'pinia'

export const useUserStore = defineStore({
    id: "use-info",
    state: () => {
        return {
            u_info: {}
        }
    },
    persist: true
})